using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Application.DTOs.Events;

public record PublishEventRequest(
    string ClientToken,
    string EventType,
    string Payload,
    string? Provider = null);

public record EventResponse(
    Guid Id,
    Guid WebhookId,
    string? Provider,
    string EventType,
    EventStatus Status,
    int Attempts,
    string? LastError,
    DateTime ReceivedAtUtc,
    DateTime? NextAttemptAtUtc,
    DateTime? ProcessedAtUtc);
