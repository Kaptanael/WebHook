using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Tokens;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Custom static-token authenticator: matches the raw <c>x-token</c> request header (no scheme, not
/// <c>Bearer</c>) against the endpoint's <see cref="WebhookEndpoint.EndPointToken"/>. Tries an exact
/// fixed-time comparison first, then falls back to comparing the two tokens' decoded <em>identity</em>
/// (ApiKey, ClientId, ClientSecret, CompanyId) so cosmetic differences in the non-secret BaseUrl /
/// ApplicationName fields don't prevent a match. The ClientSecret is still verified.
/// </summary>
public class TokenInboundAuthenticator(ITokenDecoder tokenDecoder) : IInboundAuthenticator
{
    public const string HeaderName = "x-token";

    public Result Authenticate(InboundRequest request, WebhookEndpoint endpoint)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
            return Result.Failure($"Missing {HeaderName} header.");

        if (string.IsNullOrWhiteSpace(endpoint.EndPointToken))
            return Result.Failure("Endpoint has no token configured.");

        var presented = headerValue.Trim();

        // Fast path: the token strings are byte-identical.
        if (InboundSecret.FixedTimeEquals(endpoint.EndPointToken, presented))
            return Result.Success();

        // Identity match: same credential, ignoring cosmetic BaseUrl/ApplicationName differences
        // (e.g. an externally-issued token whose host/app-name differ from the provisioned one).
        var presentedDecoded = tokenDecoder.Decode(presented);
        var endpointDecoded = tokenDecoder.Decode(endpoint.EndPointToken);
        if (presentedDecoded.IsSuccess && endpointDecoded.IsSuccess &&
            IdentityMatches(presentedDecoded.Value!, endpointDecoded.Value!))
            return Result.Success();

        return Result.Failure("Invalid token.");
    }

    private static bool IdentityMatches(TokenDecoderResponse a, TokenDecoderResponse b) =>
        a.CompanyId == b.CompanyId &&
        InboundSecret.FixedTimeEquals(a.ApiKey, b.ApiKey) &&
        InboundSecret.FixedTimeEquals(a.ClientId, b.ClientId) &&
        InboundSecret.FixedTimeEquals(a.ClientSecret, b.ClientSecret);
}
