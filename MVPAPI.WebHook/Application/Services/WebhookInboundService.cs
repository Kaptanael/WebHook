using AutoMapper;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookInboundService(
    IMapper mapper,
    WebhookSigner signer,
    IWebhookManager webhookManager,
    IWebHookConnectionRepository connectionRepository,
    IWebhookOutboundRepository endpointRepository,
    IWebhookInboundRepository eventRepository,
    ILogger<WebhookInboundService> logger) : IWebhookInboundService
{
    public async Task<Result<IReadOnlyList<WebhookInboundResponse>>> PublishEventAsync(
        EventRequest request,
        string token,
        string signature,
        string timestamp,
        CancellationToken cancellationToken = default)
    {
        var headerResult = signer.ValidateHeaders(timestamp, signature, token);
        if (!headerResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — invalid headers: {Error}", headerResult.Error);
            return Result.Failure<IReadOnlyList<WebhookInboundResponse>>(headerResult.Error!);
        }

        var connectionResult = await webhookManager.EnsureConnectionAsync(token, cancellationToken);
        if (!connectionResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — connection failed: {Error}", connectionResult.Error);
            return Result.Failure<IReadOnlyList<WebhookInboundResponse>>(connectionResult.Error!);
        }

        var decodeResult = ClientTokenConverter.Decode(token);
        if (!decodeResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — token decode failed: {Error}", decodeResult.Error);
            return Result.Failure<IReadOnlyList<WebhookInboundResponse>>(decodeResult.Error!);
        }

        var rawPayload = request.Payload.GetRawText();
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            logger.LogWarning("Event rejected — empty payload.");
            return Result.Failure<IReadOnlyList<WebhookInboundResponse>>("Payload cannot be empty.");
        }

        var signatureResult = signer.VerifySignature(decodeResult.Value!.ApiKey, timestamp, rawPayload, signature);
        if (!signatureResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — HMAC signature mismatch for event type {EventType}.", request.EventType);
            return Result.Failure<IReadOnlyList<WebhookInboundResponse>>(signatureResult.Error!);
        }

        var connection = connectionResult.Value!;
        await webhookManager.EnsureRegisteredAsync(token, request.EventType, connection.CompanyId, cancellationToken);

        var endpoints = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(
            connection.CompanyId, request.EventType, cancellationToken);

        if (endpoints.Count == 0)
        {
            logger.LogInformation("No active endpoints for company {CompanyId} with event type {EventType}; skipping.", connection.CompanyId, request.EventType);
            return Result.Success<IReadOnlyList<WebhookInboundResponse>>([]);
        }

        var events = await FanOutAndPersistAsync(connection.Id, request.Client, request.EventType, rawPayload, cancellationToken);
        logger.LogInformation("Queued {EventCount} event(s) for company {CompanyId} with event type {EventType}.", events.Count, connection.CompanyId, request.EventType);
        return Result.Success<IReadOnlyList<WebhookInboundResponse>>(mapper.Map<List<WebhookInboundResponse>>(events));
    }

    public async Task<IReadOnlyList<WebhookInboundResponse>> PublishAsync(PublishEventRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(request.ClientToken, cancellationToken)
            ?? throw new NotFoundException("No connection found for the supplied client token.");

        var endpoints = await endpointRepository.GetByCompanyIdAsync(connection.CompanyId, cancellationToken);
        if (endpoints.Count == 0)
        {
            logger.LogInformation("No endpoints for company {CompanyId}; nothing to publish.", connection.CompanyId);
            return [];
        }

        var events = await FanOutAndPersistAsync(connection.Id, request.Provider, request.EventType, request.Payload, cancellationToken);
        logger.LogInformation("Published {EventCount} event(s) for company {CompanyId} with event type {EventType}.", events.Count, connection.CompanyId, request.EventType);
        return mapper.Map<List<WebhookInboundResponse>>(events);
    }

    public async Task<IReadOnlyList<WebhookInboundResponse>> PublishToEndpointAsync(Guid webhookId, int companyId, string eventType, string payload, string? provider, CancellationToken cancellationToken = default)
    {
        var events = await FanOutAndPersistAsync(webhookId, provider, eventType, payload, cancellationToken);
        logger.LogInformation("Queued {EventCount} inbound event(s) for company {CompanyId} with event type {EventType}.", events.Count, companyId, eventType);
        return mapper.Map<List<WebhookInboundResponse>>(events);
    }

    public async Task<WebhookInboundResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        return webhookEvent is null ? null : mapper.Map<WebhookInboundResponse>(webhookEvent);
    }

    public async Task<IReadOnlyList<WebhookInboundResponse>> GetDueForProcessingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetDueForProcessingAsync(batchSize, DateTime.UtcNow, cancellationToken);
        return mapper.Map<List<WebhookInboundResponse>>(events);
    }

    public async Task<IReadOnlyList<WebhookInboundResponse>> GetFailedAsync(int limit, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetByStatusAsync(EventStatus.Failed, limit, cancellationToken);
        return mapper.Map<List<WebhookInboundResponse>>(events);
    }

    private async Task<List<WebhookInbound>> FanOutAndPersistAsync(
        Guid webhookId,
        string? provider,
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var events = new List<WebhookInbound>();

        var webhookEvent = new WebhookInbound
        {
            WebhookId = webhookId,
            Provider = provider,
            EventType = eventType,
            Payload = payload,
            Status = EventStatus.Pending,
            ReceivedAtUtc = nowUtc,
            NextAttemptAtUtc = nowUtc,
            IdempotencyKey = Guid.NewGuid()
        };
        await eventRepository.AddAsync(webhookEvent, cancellationToken);
        events.Add(webhookEvent);

        return events;
    }
}
