using MVPAPI.WebHook.Domain.Enums;
using System.Text.Json;

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

public record EventRequest(    
    string EventType,
    JsonElement Payload,
    string Client);

public class OtherEventRequestPayload
{
    public DateTime? EventUtcTime { get; set; }

    public int? UtcOffset { get; set; }

    public string DeviceId { get; set; } = null!;

    public string EventClass { get; set; } = null!;

    public string? EventDescription { get; set; }

    public string? DeviceName { get; set; } = null!;
}

