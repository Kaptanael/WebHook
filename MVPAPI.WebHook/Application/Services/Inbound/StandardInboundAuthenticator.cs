using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class StandardInboundAuthenticator(
IApiKeyRepository apiKeyRepository,
WebhookSigner signer,
ILogger<StandardInboundAuthenticator> logger) : IInboundAuthenticator
{
    private const int TimestampToleranceSeconds = 300;   

    public async Task<Result<InboundAuthResult>> AuthenticateAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Headers.TryGetValue(WebhookHeaders.ApiKeyHeader, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            return Result<InboundAuthResult>.Success(InboundAuthResult.NotPresented());

        request.Headers.TryGetValue(WebhookHeaders.IdHeader, out var id);
        request.Headers.TryGetValue(WebhookHeaders.TimestampHeader, out var timestamp);
        request.Headers.TryGetValue(WebhookHeaders.SignatureHeader, out var presentedSignatures);

        if (string.IsNullOrWhiteSpace(id))
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected($"Missing {WebhookHeaders.IdHeader} header."));

        if (string.IsNullOrWhiteSpace(timestamp))
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected($"Missing {WebhookHeaders.TimestampHeader} header."));

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected("Request timestamp is invalid or expired."));
        if (string.IsNullOrWhiteSpace(presentedSignatures))
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected($"Missing {WebhookHeaders.SignatureHeader} header."));

        var key = await apiKeyRepository.GetByRawApiKeyAsync(apiKey, cancellationToken);
        if (key is null || !key.IsActive)
        {            
            logger.LogWarning("Inbound X-Api-Key rejected — no active PortalDB key matched.");
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected("Invalid API key."));
        }

        if (string.IsNullOrWhiteSpace(key.Salt))
        {
            logger.LogError("PortalDB API key {ApiKeyId} (company {CompanyId}) has no salt; cannot verify signature.", key.Id, key.CompanyId);
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected("API key has no signing secret configured."));
        }

        // The salt is the shared secret and is used directly as the raw HMAC key.
        var expected = signer.Sign(id, DateTimeOffset.FromUnixTimeSeconds(ts), request.RawBody, key.Salt);

        foreach (var presented in presentedSignatures.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (InboundSecret.FixedTimeEquals(expected.Signature, presented))
                return Result<InboundAuthResult>.Success(InboundAuthResult.Authenticated((int)key.CompanyId, key.RawApiKey!, key.ApplicationName));
        }

        logger.LogWarning("Inbound signature mismatch for PortalDB key {ApiKeyId} (company {CompanyId}).", key.Id, key.CompanyId);
        return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected("Invalid signature."));
    }
}
