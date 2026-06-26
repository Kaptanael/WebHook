    namespace MVPAPI.WebHook.Domain.Entities;

public class WebhookOutbound
{
    public Guid Id { get; set; }

    public string EndPointToken { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    public string TriggerConfigJson { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public string ActionDataSchema { get; set; } = string.Empty;
}
