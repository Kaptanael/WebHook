using Microsoft.Extensions.Logging;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Verifies the full Standard Webhooks credential set: <c>X-Api-Key</c> selects an API key row in
/// PortalDB and the <c>webhook-id</c>/<c>webhook-timestamp</c>/<c>webhook-signature</c> triplet is
/// recomputed with the same <see cref="IWebhookSigner"/> used for outbound, keyed by the row's
/// <see cref="Domain.Entities.ApiKey.Salt"/>. The salt is the shared secret and is never sent on the wire.
/// </summary>
public class ApiKeyStandardWebhookResolver(
    IApiKeyRepository apiKeyRepository,
    IWebhookSigner signer,
    ILogger<ApiKeyStandardWebhookResolver> logger) : IApiKeyInboundResolver
{
    private const int TimestampToleranceSeconds = 300;

    public const string ApiKeyHeader = "x-api-key";
    public const string IdHeader = "webhook-id";
    public const string TimestampHeader = "webhook-timestamp";
    public const string SignatureHeader = "webhook-signature";

    public async Task<ApiKeyAuthResult> ResolveAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Headers.TryGetValue(ApiKeyHeader, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            return ApiKeyAuthResult.NotPresented();

        request.Headers.TryGetValue(IdHeader, out var id);
        request.Headers.TryGetValue(TimestampHeader, out var timestamp);
        request.Headers.TryGetValue(SignatureHeader, out var presentedSignatures);

        if (string.IsNullOrWhiteSpace(id))
            return ApiKeyAuthResult.Rejected($"Missing {IdHeader} header.");

        if (string.IsNullOrWhiteSpace(timestamp))
            return ApiKeyAuthResult.Rejected($"Missing {TimestampHeader} header.");

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return ApiKeyAuthResult.Rejected("Request timestamp is invalid or expired.");

        if (string.IsNullOrWhiteSpace(presentedSignatures))
            return ApiKeyAuthResult.Rejected($"Missing {SignatureHeader} header.");

        var key = await apiKeyRepository.GetByRawApiKeyAsync(apiKey, cancellationToken);
        if (key is null || !key.IsActive)
        {
            // Don't reveal which of the two (unknown vs inactive) failed.
            logger.LogWarning("Inbound X-Api-Key rejected — no active PortalDB key matched.");
            return ApiKeyAuthResult.Rejected("Invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(key.Salt))
        {
            logger.LogError("PortalDB API key {ApiKeyId} (company {CompanyId}) has no salt; cannot verify signature.", key.Id, key.CompanyId);
            return ApiKeyAuthResult.Rejected("API key has no signing secret configured.");
        }

        // The salt is base64. Prefix it so the signer base64-decodes it to the raw HMAC key bytes,
        // keeping verification keyed identically to outbound signing. Reject loudly on a non-base64
        // salt rather than letting the signer silently fall back to raw UTF-8 (which would never match).
        if (!IsBase64(key.Salt))
        {
            logger.LogError("PortalDB API key {ApiKeyId} (company {CompanyId}) salt is not valid base64.", key.Id, key.CompanyId);
            return ApiKeyAuthResult.Rejected("API key salt is not valid base64.");
        }

        var signingSecret = WebhookSigningSecret.Prefix + key.Salt;
        var expected = signer.Sign(id, DateTimeOffset.FromUnixTimeSeconds(ts), request.RawBody, signingSecret);

        foreach (var presented in presentedSignatures.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (InboundSecret.FixedTimeEquals(expected.Signature, presented))
                return ApiKeyAuthResult.Authenticated(key.CompanyId, key.RawApiKey);
        }

        logger.LogWarning("Inbound signature mismatch for PortalDB key {ApiKeyId} (company {CompanyId}).", key.Id, key.CompanyId);
        return ApiKeyAuthResult.Rejected("Invalid signature.");
    }

    private static bool IsBase64(string value) =>
        Convert.TryFromBase64String(value, new byte[((value.Length + 3) / 4) * 3], out _);
}
