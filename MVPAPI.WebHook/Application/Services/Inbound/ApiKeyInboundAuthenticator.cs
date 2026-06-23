using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// API-key authenticator: compares the <c>X-Api-Key</c> request header against the endpoint's
/// <see cref="WebhookEndpoint.EndPointToken"/> using a fixed-time comparison.
/// </summary>
public class ApiKeyInboundAuthenticator : IInboundAuthenticator
{
    public const string HeaderName = "x-api-key";

    public Result Authenticate(InboundRequest request, WebhookEndpoint endpoint)
    {
        if (!request.Headers.TryGetValue(HeaderName, out var presentedKey) || string.IsNullOrWhiteSpace(presentedKey))
            return Result.Failure($"Missing {HeaderName} header.");

        if (string.IsNullOrWhiteSpace(endpoint.EndPointToken))
            return Result.Failure("Endpoint has no token configured.");

        return InboundSecret.FixedTimeEquals(endpoint.EndPointToken, presentedKey)
            ? Result.Success()
            : Result.Failure("Invalid API key.");
    }
}
