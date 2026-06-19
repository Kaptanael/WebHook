namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookSignatureVerifier
{
    /// <summary>
    /// Validates the presence and freshness of the request headers.
    /// Returns an error message when invalid, or <c>null</c> when the headers are acceptable.
    /// </summary>
    string? ValidateHeaders(string timestamp, string signature, string token);

    /// <summary>
    /// Verifies the HMAC signature of the payload against the supplied API key.
    /// </summary>
    bool VerifySignature(string apiKey, string timestamp, string rawPayload, string signature);
}
