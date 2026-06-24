using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Verifies the Standard Webhooks signing triplet (<c>webhook-id</c>, <c>webhook-timestamp</c>,
/// <c>webhook-signature</c>) against the endpoint's <see cref="WebhookEndpoint.SigningSecret"/>.
/// Recomputes the expected signature with the same <see cref="IWebhookSigner"/> used for outbound,
/// so signing and verification never drift, and accepts any of the space-delimited <c>v1,...</c>
/// signatures (supporting secret rotation).
/// </summary>
public class StandardWebhooksInboundAuthenticator(IWebhookSigner signer) : IInboundAuthenticator
{
    private const int TimestampToleranceSeconds = 300;

    public const string IdHeader = "webhook-id";
    public const string TimestampHeader = "webhook-timestamp";
    public const string SignatureHeader = "webhook-signature";

    public Result Authenticate(InboundRequest request, WebhookEndpoint endpoint)
    {
        var secret = endpoint.SigningSecret;
        if (string.IsNullOrWhiteSpace(secret))
            return Result.Failure("Endpoint has no signing secret configured.");

        request.Headers.TryGetValue(IdHeader, out var id);
        request.Headers.TryGetValue(TimestampHeader, out var timestamp);
        request.Headers.TryGetValue(SignatureHeader, out var presentedSignatures);

        if (string.IsNullOrWhiteSpace(id))
            return Result.Failure($"Missing {IdHeader} header.");

        if (string.IsNullOrWhiteSpace(timestamp))
            return Result.Failure($"Missing {TimestampHeader} header.");

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return Result.Failure("Request timestamp is invalid or expired.");

        if (string.IsNullOrWhiteSpace(presentedSignatures))
            return Result.Failure($"Missing {SignatureHeader} header.");

        var expected = signer.Sign(id, DateTimeOffset.FromUnixTimeSeconds(ts), request.RawBody, secret);

        foreach (var presented in presentedSignatures.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (InboundSecret.FixedTimeEquals(expected.Signature, presented))
                return Result.Success();
        }

        return Result.Failure("Invalid signature.");
    }
}
