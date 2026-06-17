namespace MVPAPI.WebHook.Application.DTOs.Events;

public record MvpEventResponse(
    Guid Id,
    int? Priority,
    int? Category,
    int? PanelNo,
    DateTime? EventDate,
    int? DeviceNo,
    int? Status,
    int? FacilityNo,
    long? Badge,
    string? Class,
    string? Description,
    string? Name,
    int? Archive,
    Guid? AcknowledgeOperator,
    DateTime? AcknowledgeTimestamp,
    string? Actions,
    bool? ResponseRequired,
    Guid? CaObjectId,
    long? Tag,
    bool HasPhoto,
    bool? HasVideo,
    bool? Pending,
    int? UtcOffset,
    int? Sphere,
    int CompanyId,
    int SeqNoFromLock,
    int RecordCount);

public class CreateMVPEventPayload
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

    public int CompanyId { get; set; }
}

