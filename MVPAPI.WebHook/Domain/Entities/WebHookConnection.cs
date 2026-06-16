namespace MVPAPI.WebHook.Domain.Entities;

public class WebHookConnection
{
    public Guid Id { get; set; }
    public int CompanyId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ClientToken { get; set; } = string.Empty;
    public bool IsActive { get; set; } 
    public string MVPApiToken { get; set; } = string.Empty;
    public string MVPApiRefreshToken { get; set; } = string.Empty;
    public DateTime MVPApiExpiresIn { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
