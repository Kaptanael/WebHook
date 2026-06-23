namespace MVPAPI.WebHook.Domain.Entities;

/// <summary>
/// A row from <c>[PortalDB].[dbo].[ClientCredentials]</c>. Holds the OAuth client credentials used to
/// obtain an MVP API token for a company. Looked up by <see cref="CompanyId"/> when auto-provisioning a
/// <see cref="WebHookConnection"/> for an inbound API key that has no connection yet.
/// </summary>
public class ClientCredential
{
    public long Id { get; set; }

    public int CompanyId { get; set; }

    public string ClientId { get; set; } = string.Empty;

    /// <summary>Plaintext client secret (the <c>Secret</c> column), used to request the MVP API token.</summary>
    public string Secret { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime? ExpiryUtc { get; set; }
}
