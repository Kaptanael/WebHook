using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Domain.Entities;

public class WebhookEvent
{
    public Guid Id { get; set; }
    public Guid WebhookId { get; set; }
    public string? Provider { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public EventStatus Status { get; set; } = EventStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? ProcessingStartedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public Guid IdempotencyKey { get; set; }
}
