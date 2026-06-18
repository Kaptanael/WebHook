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
    ITokenDecoder tokenDecoder,
    IWebHookConnectionManager connectionManager,
    IOptions<WebhookRouteOptions> routeOptions,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebhookEventRepository eventRepository) : IWebhookEventService
{
    private const int MaxAttempts = 5;
    private const int TimestampToleranceSeconds = 300;

    public async Task<Result<IReadOnlyList<EventResponse>>> PublishEventAsync(
        EventRequest request,
        string token,
        string signature,
        string timestamp,
        CancellationToken cancellationToken = default)
    {
        var headerError = ValidateRequestHeaders(timestamp, signature, token);
        if (headerError is not null)
            return Result.Failure<IReadOnlyList<EventResponse>>(headerError);

        var connectionResult = await connectionManager.EnsureConnectionAsync(token, cancellationToken);
        if (!connectionResult.IsSuccess)
            return Result.Failure<IReadOnlyList<EventResponse>>(connectionResult.Error!);

        var decodeResult = tokenDecoder.Decode(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<IReadOnlyList<EventResponse>>(decodeResult.Error!);

        var rawPayload = request.Payload.GetRawText();
        if (string.IsNullOrWhiteSpace(rawPayload))
            return Result.Failure<IReadOnlyList<EventResponse>>("Payload cannot be empty.");

        if (!VerifyHmacSignature(decodeResult.Value!.ApiKey, timestamp, rawPayload, signature))
            return Result.Failure<IReadOnlyList<EventResponse>>("Invalid signature.");

        var connection = connectionResult.Value!;
        await EnsureEndpointRegisteredAsync(token, request.EventType, connection.CompanyId, cancellationToken);

        var endpoints = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(
            connection.CompanyId, request.EventType, cancellationToken);

        if (endpoints.Count == 0)
            return Result.Success<IReadOnlyList<EventResponse>>([]);

        var events = await PersistEventsAsync(endpoints, request, rawPayload, cancellationToken);
        return Result.Success<IReadOnlyList<EventResponse>>(mapper.Map<List<EventResponse>>(events));
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

    private static string? ValidateRequestHeaders(string timestamp, string signature, string token)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return "Missing X-Timestamp header.";

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return "Request timestamp is invalid or expired.";

        if (string.IsNullOrWhiteSpace(signature))
            return "Missing X-Signature header.";

        if (string.IsNullOrWhiteSpace(token))
            return "Missing X-Endpoint-Token header.";

        return null;
    }

    private static bool VerifyHmacSignature(string apiKey, string timestamp, string rawPayload, string signature)
    {
        var expected = ComputeHmac(apiKey, $"{timestamp}.{rawPayload}");
        return CryptographicEquals(expected, signature);
    }

    private async Task EnsureEndpointRegisteredAsync(string token, string eventType, int companyId, CancellationToken cancellationToken)
    {
        if (!routeOptions.Value.Routes.TryGetValue(eventType, out var internalUrl))
            return;

        var existing = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(companyId, eventType, cancellationToken);
        if (existing.Count > 0)
            return;

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

    private async Task<List<WebhookEvent>> PersistEventsAsync(
        IReadOnlyList<WebhookEndpoint> endpoints, EventRequest request, string rawPayload,
        CancellationToken cancellationToken)
    {
        var events = new List<WebhookEvent>(endpoints.Count);
        foreach (var endpoint in endpoints)
        {
            var webhookEvent = new WebhookEvent
            {
                WebhookId = endpoint.Id,
                Provider = request.Client,
                EventType = request.EventType,
                Payload = rawPayload,
                Status = EventStatus.Pending,
                ReceivedAtUtc = DateTime.UtcNow,
                NextAttemptAtUtc = DateTime.UtcNow
            };
            events.Add(webhookEvent);
            await eventRepository.AddAsync(webhookEvent, cancellationToken);
        }
        return events;
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
