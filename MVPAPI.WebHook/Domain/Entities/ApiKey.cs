namespace MVPAPI.WebHook.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    public string ApiKeyHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string? AllowedIPs { get; set; }
    public string? Scopes { get; set; }
    public string Status { get; set; } = string.Empty;
    public byte Environment { get; set; }
    public string? JsonConfig { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public long IssuedBy { get; set; }
    public long? UpdatedBy { get; set; }
    public string? RawApiKey { get; set; }
    public Guid? AppRefId { get; set; }
    public string? ApplicationType { get; set; }
    public bool IsActive =>
        Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ||
        Status is "1";
}
