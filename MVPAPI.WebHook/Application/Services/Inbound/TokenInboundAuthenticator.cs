using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class TokenInboundAuthenticator( 
IApiKeyRepository apiKeyRepository,
ILogger<TokenInboundAuthenticator> logger) : IInboundAuthenticator
{
    public async Task<Result<InboundAuthResult>> AuthenticateAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Headers.TryGetValue(WebhookHeaders.TokenHeader, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
            return Result<InboundAuthResult>.Success(InboundAuthResult.NotPresented());        

        var presentedClientToken = headerValue.Trim();
        var presentedDecoded = ClientTokenConverter.Decode(presentedClientToken);        

        var key = await apiKeyRepository.GetByRawApiKeyAsync(presentedDecoded.Value!.ApiKey, cancellationToken);
        if (key is null || !key.IsActive)
        {            
            logger.LogWarning("Inbound X-Api-Key rejected — no active PortalDB key matched.");
            return Result<InboundAuthResult>.Success(InboundAuthResult.Rejected("Invalid API key."));
        }

        logger.LogInformation("Inbound X-Api-Key matched for CompanyId {CompanyId}, ApplicationName {ApplicationName}.", key.CompanyId, key.ApplicationName);
        return Result<InboundAuthResult>.Success(InboundAuthResult.Authenticated((int)key.CompanyId, key.RawApiKey!, key.ApplicationName));
    }

    private static bool IdentityMatches(DecodedClientToken a, DecodedClientToken b) =>
        a.CompanyId == b.CompanyId &&
        InboundSecret.FixedTimeEquals(a.ApiKey, b.ApiKey) &&
        InboundSecret.FixedTimeEquals(a.ClientId, b.ClientId) &&
        InboundSecret.FixedTimeEquals(a.ClientSecret, b.ClientSecret);
}
