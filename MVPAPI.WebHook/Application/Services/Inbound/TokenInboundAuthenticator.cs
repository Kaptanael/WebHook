using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Custom static-token authenticator: compares the raw <c>token</c> request header (no scheme,
/// not <c>Bearer</c>) against the endpoint's <see cref="WebhookEndpoint.EndPointToken"/> using a
/// fixed-time comparison.
/// </summary>
public class TokenInboundAuthenticator : IInboundAuthenticator
{
    public const string HeaderName = "token";

    public Result Authenticate(InboundRequest request, WebhookEndpoint endpoint)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var headerValue) || string.IsNullOrWhiteSpace(headerValue))
            return Result.Failure($"Missing {HeaderName} header.");

        if (string.IsNullOrWhiteSpace(endpoint.EndPointToken))
            return Result.Failure("Endpoint has no token configured.");

        return InboundSecret.FixedTimeEquals(endpoint.EndPointToken, headerValue.Trim())
            ? Result.Success()
            : Result.Failure("Invalid token.");
    }
}
