using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    private const int TimestampToleranceSeconds = 300;

    public Result ValidateHeaders(string timestamp, string signature, string token)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
            return Result.Failure("Missing X-Timestamp header.");

        if (!long.TryParse(timestamp, out var ts) ||
            Math.Abs(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts) > TimestampToleranceSeconds)
            return Result.Failure("Request timestamp is invalid or expired.");

        if (string.IsNullOrWhiteSpace(signature))
            return Result.Failure("Missing X-Signature header.");

        if (string.IsNullOrWhiteSpace(token))
            return Result.Failure("Missing X-Endpoint-Token header.");

        return Result.Success();
    }

    public Result VerifySignature(string apiKey, string timestamp, string rawPayload, string signature)
    {
        var expected = ComputeHmac(apiKey, $"{timestamp}.{rawPayload}");
        return CryptographicEquals(expected, signature)
            ? Result.Success()
            : Result.Failure("Invalid signature.");
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
