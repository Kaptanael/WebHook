using MVPAPI.WebHook.Domain.Enums;
using System.ComponentModel;
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
    string Provider);

public class OtherEventRequestPayload
{
    public DateTime? EventUtcTime { get; set; }

    public int? UtcOffset { get; set; }

    public string DeviceId { get; set; } = null!;

    public string EventClass { get; set; } = null!;

    public string? EventDescription { get; set; }

    public string? DeviceName { get; set; } = null!;
}

public class CreateEventPayload
{    
    public int? Priority { get; set; }
    
    public int? Category { get; set; }
    
    public int? PanelId { get; set; }
    
    public DateTime? EventDate { get; set; }
    
    public int? DeviceId { get; set; }
    
    public int? Status { get; set; }
    
    public int? FacilityNo { get; set; }
    
    public long? Badge { get; set; }
    
    public string? Class { get; set; }
    
    public string? Description { get; set; }
    
    public string? Name { get; set; }
    
    public int? Archive { get; set; }
    
    public Guid? AcknowledgeOperator { get; set; }
    
    public DateTime? AcknowledgeTimeStamp { get; set; }
    
    public string? Actions { get; set; }
    
    public bool? ResponseRequired { get; set; }
    
    public long? Tag { get; set; }
    
    public bool HasPhoto { get; set; } = false;
    
    public bool? HasVideo { get; set; }
    
    public bool? Pending { get; set; }
    
    public int? Sphere { get; set; }
    
    public int SequenceNoFromLock { get; set; }
    
    public int RecordCount { get; set; }
}
