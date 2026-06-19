using AutoMapper;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEventService(
    IMapper mapper,
    ITokenDecoder tokenDecoder,
    IWebhookSignatureVerifier signatureVerifier,
    IWebHookConnectionManager connectionManager,
    IOptions<WebhookRouteOptions> routeOptions,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebhookEventRepository eventRepository,
    ILogger<WebhookEventService> logger) : IWebhookEventService
{
    public async Task<Result<IReadOnlyList<EventResponse>>> PublishEventAsync(
        EventRequest request,
        string token,
        string signature,
        string timestamp,
        CancellationToken cancellationToken = default)
    {
        var headerResult = signatureVerifier.ValidateHeaders(timestamp, signature, token);
        if (!headerResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — invalid headers: {Error}", headerResult.Error);
            return Result.Failure<IReadOnlyList<EventResponse>>(headerResult.Error!);
        }

        var connectionResult = await connectionManager.EnsureConnectionAsync(token, cancellationToken);
        if (!connectionResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — connection failed: {Error}", connectionResult.Error);
            return Result.Failure<IReadOnlyList<EventResponse>>(connectionResult.Error!);
        }

        var decodeResult = tokenDecoder.Decode(token);
        if (!decodeResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — token decode failed: {Error}", decodeResult.Error);
            return Result.Failure<IReadOnlyList<EventResponse>>(decodeResult.Error!);
        }

        var rawPayload = request.Payload.GetRawText();
        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            logger.LogWarning("Event rejected — empty payload.");
            return Result.Failure<IReadOnlyList<EventResponse>>("Payload cannot be empty.");
        }

        var signatureResult = signatureVerifier.VerifySignature(decodeResult.Value!.ApiKey, timestamp, rawPayload, signature);
        if (!signatureResult.IsSuccess)
        {
            logger.LogWarning("Event rejected — HMAC signature mismatch for event type {EventType}.", request.EventType);
            return Result.Failure<IReadOnlyList<EventResponse>>(signatureResult.Error!);
        }

        var connection = connectionResult.Value!;
        await EnsureEndpointRegisteredAsync(token, request.EventType, connection.CompanyId, cancellationToken);

        var endpoints = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(
            connection.CompanyId, request.EventType, cancellationToken);

        if (endpoints.Count == 0)
        {
            logger.LogInformation("No active endpoints for company {CompanyId} with event type {EventType}; skipping.", connection.CompanyId, request.EventType);
            return Result.Success<IReadOnlyList<EventResponse>>([]);
        }

        var events = await FanOutAndPersistAsync(endpoints, request.Client, request.EventType, rawPayload, cancellationToken);
        logger.LogInformation("Queued {EventCount} event(s) for company {CompanyId} with event type {EventType}.", events.Count, connection.CompanyId, request.EventType);
        return Result.Success<IReadOnlyList<EventResponse>>(mapper.Map<List<EventResponse>>(events));
    }

    public async Task<IReadOnlyList<EventResponse>> PublishAsync(PublishEventRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(request.ClientToken, cancellationToken)
            ?? throw new NotFoundException("No connection found for the supplied client token.");

        var endpoints = await endpointRepository.GetByCompanyIdAsync(connection.CompanyId, cancellationToken);
        if (endpoints.Count == 0)
        {
            logger.LogInformation("No endpoints for company {CompanyId}; nothing to publish.", connection.CompanyId);
            return [];
        }

        var events = await FanOutAndPersistAsync(endpoints, request.Provider, request.EventType, request.Payload, cancellationToken);
        logger.LogInformation("Published {EventCount} event(s) for company {CompanyId} with event type {EventType}.", events.Count, connection.CompanyId, request.EventType);
        return mapper.Map<List<EventResponse>>(events);
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        return webhookEvent is null ? null : mapper.Map<EventResponse>(webhookEvent);
    }

    public async Task<IReadOnlyList<EventResponse>> GetDueForProcessingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetDueForProcessingAsync(batchSize, DateTime.UtcNow, cancellationToken);
        return mapper.Map<List<EventResponse>>(events);
    }

    private async Task EnsureEndpointRegisteredAsync(string token, string eventType, int companyId, CancellationToken cancellationToken)
    {
        if (!routeOptions.Value.Routes.TryGetValue(eventType, out var internalUrl))
            return;

        var existing = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(companyId, eventType, cancellationToken);
        if (existing.Count > 0)
            return;

        logger.LogInformation("Auto-registering internal endpoint for company {CompanyId} with event type {EventType} -> {InternalUrl}.", companyId, eventType, internalUrl);
        await endpointRepository.AddAsync(new WebhookEndpoint
        {
            EndPointToken = token,
            Endpoint = internalUrl,
            CompanyId = companyId,
            TriggerConfigJson = JsonSerializer.Serialize(new { triggerType = eventType, companyId }),
            IsActive = true,
            ActionDataSchema = "{}"
        }, cancellationToken);
    }

    private async Task<List<WebhookEvent>> FanOutAndPersistAsync(
        IReadOnlyList<WebhookEndpoint> endpoints, string? provider, string eventType, string payload,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var events = new List<WebhookEvent>(endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            var webhookEvent = new WebhookEvent
            {
                WebhookId        = endpoint.Id,
                Provider         = provider,
                EventType        = eventType,
                Payload          = payload,
                Status           = EventStatus.Pending,
                ReceivedAtUtc    = nowUtc,
                NextAttemptAtUtc = nowUtc,
                IdempotencyKey   = Guid.NewGuid().ToString()
            };
            await eventRepository.AddAsync(webhookEvent, cancellationToken);
            events.Add(webhookEvent);
        }
        return events;
    }
}
