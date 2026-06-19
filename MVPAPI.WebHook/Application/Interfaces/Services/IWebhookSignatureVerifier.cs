using MVPAPI.WebHook.Application.Common;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookSignatureVerifier
{
    /// <summary>
    /// Validates the presence and freshness of the request headers.
    /// Returns a failed <see cref="Result"/> carrying the reason when invalid, otherwise success.
    /// </summary>
    Result ValidateHeaders(string timestamp, string signature, string token);

    /// <summary>
    /// Verifies the HMAC signature of the payload against the supplied API key.
    /// </summary>
    Result VerifySignature(string apiKey, string timestamp, string rawPayload, string signature);
}
