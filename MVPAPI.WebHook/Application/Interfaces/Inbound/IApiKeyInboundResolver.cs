namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

public enum ApiKeyAuthOutcome
{
    /// <summary>No <c>X-Api-Key</c> header — this method does not apply; try the others.</summary>
    NotPresented,
    /// <summary>Key matched and the Standard Webhooks signature verified against its salt.</summary>
    Authenticated,
    /// <summary>An <c>X-Api-Key</c> was presented but the key or signature was invalid — reject; do not fall through.</summary>
    Rejected
}

public sealed record ApiKeyAuthResult(ApiKeyAuthOutcome Outcome, int CompanyId = 0, string? RawApiKey = null, string? Error = null)
{
    public static ApiKeyAuthResult NotPresented() => new(ApiKeyAuthOutcome.NotPresented);
    public static ApiKeyAuthResult Authenticated(int companyId, string rawApiKey) => new(ApiKeyAuthOutcome.Authenticated, companyId, rawApiKey);
    public static ApiKeyAuthResult Rejected(string error) => new(ApiKeyAuthOutcome.Rejected, Error: error);
}

/// <summary>
/// "Full" Standard Webhooks inbound authentication: the <c>X-Api-Key</c> header identifies an API key
/// in PortalDB, and the <c>webhook-id</c>/<c>webhook-timestamp</c>/<c>webhook-signature</c> triplet is
/// verified against that key's salt (the shared secret). The salt never travels on the wire.
/// </summary>
public interface IApiKeyInboundResolver
{
    Task<ApiKeyAuthResult> ResolveAsync(InboundRequest request, CancellationToken cancellationToken = default);
}
