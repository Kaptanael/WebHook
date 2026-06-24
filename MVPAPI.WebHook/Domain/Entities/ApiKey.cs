namespace MVPAPI.WebHook.Domain.Entities;

/// <summary>
/// A row from the external <c>[PortalDB].[dbo].[ApiKeys]</c> table. The caller presents the
/// plaintext key in the <c>X-Api-Key</c> header (matched against <see cref="RawApiKey"/>); the
/// per-key <see cref="Salt"/> is the shared secret used to verify the inbound Standard Webhooks
/// signature. Only the columns the inbound pipeline needs are mapped.
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; }

    /// <summary>Plaintext API key value matched against the <c>X-Api-Key</c> header.</summary>
    public string RawApiKey { get; set; } = string.Empty;

    /// <summary>Per-key secret used as the HMAC-SHA256 key for signature verification.</summary>
    public string Salt { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    /// <summary>Selected as text so the underlying column may be an int or a string.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Treats common "enabled" representations as active so a status flag can't silently let a revoked key through.</summary>
    public bool IsActive =>
        Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
        Status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ||
        Status is "1";
}
