using AutoMapper;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEventService(
    IMapper mapper,
    ITokenValidator tokenValidator,
    IWebHookConnectionManager connectionManager,
    IOptions<WebhookRouteOptions> routeOptions,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebhookEventRepository eventRepository,
    IOutboxMessageRepository outboxRepository) : IWebhookEventService
{
    private const int MaxAttempts = 5;
    private const int TimestampToleranceSeconds = 300;

    public async Task<Result<OutboxResponse>> PublishEventAsync(
        EventRequest request,
        string token,
        string signature,
        string timestamp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return Result.Failure<OutboxResponse>("Missing X-Timestamp header.");

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return Result.Failure<OutboxResponse>("Request timestamp is invalid or expired.");

        if (string.IsNullOrWhiteSpace(signature))
            return Result.Failure<OutboxResponse>("Missing X-Signature header.");

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<OutboxResponse>("Missing X-Endpoint-Token header.");

        var connectionResult = await connectionManager.EnsureConnectionAsync(token, cancellationToken);
        if (!connectionResult.IsSuccess)
            return Result.Failure<OutboxResponse>(connectionResult.Error!);

        var connection = connectionResult.Value!;

        var decodeResult = tokenValidator.DecodeToken(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<OutboxResponse>(decodeResult.Error!);

        var decoded = decodeResult.Value!;

        var rawPayload = request.Payload.GetRawText();
        if (string.IsNullOrWhiteSpace(rawPayload))
            return Result.Failure<OutboxResponse>("Payload cannot be empty.");

        var expected = ComputeHmac(decoded.ApiKey, $"{timestamp}.{rawPayload}");
        if (!CryptographicEquals(expected, signature))
            return Result.Failure<OutboxResponse>("Invalid signature.");

        await EnsureEndpointAsync(request.EventType, connection.CompanyId, cancellationToken);

        var message = new OutboxMessage
        {
            EventType    = request.EventType,
            Payload      = rawPayload,
            Provider     = request.Client,
            CompanyId    = connection.CompanyId,
            CreatedAtUtc = DateTime.UtcNow
        };
        await outboxRepository.AddAsync(message, cancellationToken);

        return Result.Success(new OutboxResponse(message.Id, message.EventType, message.CreatedAtUtc));
    }

    public async Task<IReadOnlyList<EventResponse>> PublishAsync(PublishEventRequest request, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(request.ClientToken, cancellationToken)
            ?? throw new NotFoundException("No connection found for the supplied client token.");

        var endpoints = await endpointRepository.GetByCompanyIdAsync(connection.CompanyId, cancellationToken);
        if (endpoints.Count == 0)
        {
            return [];
        }

        var events = new List<WebhookEvent>(endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            var webhookEvent = new WebhookEvent
            {
                WebhookId = endpoint.Id,
                Provider = request.Provider,
                EventType = request.EventType,
                Payload = request.Payload,
                Status = EventStatus.Pending,
                ReceivedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow
            };
            events.Add(webhookEvent);
            await eventRepository.AddAsync(webhookEvent, cancellationToken);
        }

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

    public async Task<bool> MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Status = EventStatus.Processing;
        webhookEvent.ProcessingStartedAtUtc = DateTime.UtcNow;
        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Status = EventStatus.Completed;
        webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
        webhookEvent.NextAttemptAtUtc = null;
        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Attempts++;
        webhookEvent.LastError = error;

        if (webhookEvent.Attempts >= MaxAttempts)
        {
            webhookEvent.Status = EventStatus.Failed;
            webhookEvent.NextAttemptAtUtc = null;
        }
        else
        {
            webhookEvent.Status = EventStatus.Retrying;
            webhookEvent.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(Math.Pow(2, webhookEvent.Attempts - 1));
        }

        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    private async Task EnsureEndpointAsync(string eventType, int companyId, CancellationToken cancellationToken)
    {
        if (!routeOptions.Value.Routes.TryGetValue(eventType, out var internalUrl))
            return;

        var existing = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(companyId, eventType, cancellationToken);
        if (existing.Count > 0)
            return;

        var triggerConfig = JsonSerializer.Serialize(new { triggerType = eventType, companyId });

        try
        {
            await endpointRepository.AddAsync(new WebhookEndpoint
            {
                EndPointToken    = TokenGenerator.Generate(),
                Endpoint         = internalUrl,
                CompanyId        = companyId,
                TriggerConfigJson = triggerConfig,
                IsActive         = true,
                ActionDataSchema = "{}"
            }, cancellationToken);
        }
        catch
        {
            // A concurrent request already created the endpoint — safe to ignore.
        }
    }

    private static string ComputeHmac(string secret, string message)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(data)).ToLowerInvariant();
    }

    private static bool CryptographicEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}
