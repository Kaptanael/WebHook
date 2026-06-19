using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private const int TimestampToleranceSeconds = 300;

    public string? ValidateHeaders(string timestamp, string signature, string token)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return "Missing X-Timestamp header.";

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return "Request timestamp is invalid or expired.";

        if (string.IsNullOrWhiteSpace(signature))
            return "Missing X-Signature header.";

        if (string.IsNullOrWhiteSpace(token))
            return "Missing X-Endpoint-Token header.";

        return null;
    }

    public bool VerifySignature(string apiKey, string timestamp, string rawPayload, string signature)
    {
        var expected = ComputeHmac(apiKey, $"{timestamp}.{rawPayload}");
        return CryptographicEquals(expected, signature);
    }

    private static string ComputeHmac(string secret, string message)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(message);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(data)).ToLowerInvariant();
    }

    private static bool CryptographicEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}
