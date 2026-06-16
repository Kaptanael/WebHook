namespace MVPAPI.WebHook.Domain.Enums;

public enum EventStatus : byte
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Retrying = 5
}
