namespace MVPAPI.WebHook.Domain.Entities;

public class MvpEvent
{
    public Guid Seqno { get; set; }
    public int? Priority { get; set; }
    public int? Cat { get; set; }
    public int? PnlNo { get; set; }
    public DateTime? EDate { get; set; }
    public int? DeviceNo { get; set; }
    public int? Status { get; set; }
    public int? Facno { get; set; }
    public long? Badge { get; set; }
    public string? Class { get; set; }
    public string? Description { get; set; }
    public string? Name { get; set; }
    public int? Arch { get; set; }
    public Guid? AckOpr { get; set; }
    public DateTime? AckTStamp { get; set; }
    public string? Actions { get; set; }
    public bool? RespReq { get; set; }
    public Guid? CaObjectID { get; set; }
    public long? Tag { get; set; }
    public bool HasPhoto { get; set; }
    public bool? HasVideo { get; set; }
    public bool? Pending { get; set; }
    public int? UTCOffset { get; set; }
    public int? Sphere { get; set; }
    public int CompanyId { get; set; }
    public int SeqNoFromLock { get; set; }
    public int RecordCount { get; set; }
}
