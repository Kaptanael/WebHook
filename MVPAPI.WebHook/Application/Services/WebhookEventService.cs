using AutoMapper;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Endpoints;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEventService(
    ITokenValidator tokenValidator,
    IMapper mapper,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebhookEventRepository eventRepository,
    IMvpEventRepository mvpEventRepository) : IWebhookEventService
{
    private const int MaxAttempts = 5;
    private const int TimestampToleranceSeconds = 300;

    public async Task<Result<EventResponse>> PublishEventAsync(
        EventRequest request,
        string? token,
        string? xSignature,
        string? xTimestamp,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(xTimestamp))
            return Result.Failure<EventResponse>("Missing X-Timestamp header.");

        if (!long.TryParse(xTimestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return Result.Failure<EventResponse>("Request timestamp is invalid or expired.");

        if (string.IsNullOrWhiteSpace(xSignature))
            return Result.Failure<EventResponse>("Missing X-Signature header.");

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure<EventResponse>("Missing X-Endpoint-Token header.");

        var decodeResult = tokenValidator.DecodeToken(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<EventResponse>(decodeResult.Error!);

        var decoded = decodeResult.Value!;

        var rawPayload = request.Payload.GetRawText();

        var expected = ComputeHmac(decoded.ApiKey, $"{xTimestamp}.{rawPayload}");
        if (!CryptographicEquals(expected, xSignature))
            return Result.Failure<EventResponse>("Invalid signature.");

        string? eventType = request.Payload.TryGetProperty("eventType", out var et) ? et.GetString() : null;
        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<EventResponse>("Payload must contain an 'eventType' property.");

        using var doc = JsonDocument.Parse(rawPayload);
        var payload = request.Payload.GetProperty("payload");

        if (eventType == "event.create")
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var createEventPayload = JsonSerializer.Deserialize<CreateMVPEventPayload>(payload, options);

            var eventToCreate = mapper.Map<MvpEvent>(createEventPayload);
            await mvpEventRepository.AddAsync(eventToCreate);
        }

        var webhookEvent = new WebhookEvent
        {
            WebhookId = Guid.NewGuid(),
            Provider = request.Client,
            EventType = request.EventType,
            Payload = rawPayload,
            Status = EventStatus.Pending,
            ReceivedAtUtc = DateTime.UtcNow,
            NextAttemptAtUtc = DateTime.UtcNow
        };
        await eventRepository.AddAsync(webhookEvent, cancellationToken);
        return Result.Success<EventResponse>(ToResponse(webhookEvent));
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

        return events.Select(ToResponse).ToList();
    }

    public async Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        return webhookEvent is null ? null : ToResponse(webhookEvent);
    }

    public async Task<IReadOnlyList<EventResponse>> GetDueForProcessingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var events = await eventRepository.GetDueForProcessingAsync(batchSize, DateTime.UtcNow, cancellationToken);
        return events.Select(ToResponse).ToList();
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

    private static EventResponse ToResponse(WebhookEvent webhookEvent) =>
        new(webhookEvent.Id,
            webhookEvent.WebhookId,
            webhookEvent.Provider,
            webhookEvent.EventType,
            webhookEvent.Status,
            webhookEvent.Attempts,
            webhookEvent.LastError,
            webhookEvent.ReceivedAtUtc,
            webhookEvent.NextAttemptAtUtc,
            webhookEvent.ProcessedAtUtc);
}
