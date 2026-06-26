using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Application.Common;

// The Standard Webhooks signing headers for an outbound request:
// webhook-id, webhook-timestamp, and webhook-signature.
public record WebhookSignatureHeaders(string Id, string Timestamp, string Signature);

// Signs outbound deliveries with the Standard Webhooks scheme (https://www.standardwebhooks.com/).
// The signed content is {id}.{timestamp}.{payload}, HMAC-SHA256 keyed with the endpoint's shared
// secret and emitted as the versioned v1,<base64> form.
//
// A whsec_-prefixed secret is the canonical Standard Webhooks form: the prefix is stripped and the
// remainder base64-decoded to obtain the key bytes, so off-the-shelf third-party verifier libraries
// interoperate. Any other secret is keyed as raw UTF-8 bytes.
//
// Also verifies the legacy token scheme (ValidateHeaders/VerifySignature): header presence + freshness,
// and a hex HMAC-SHA256 over {timestamp}.{payload} keyed with the raw UTF-8 API key.
public class WebhookSigner
{
    private const string SecretPrefix = "whsec_";
    private const int TimestampToleranceSeconds = 300;

    // Signs an outbound webhook payload using the Standard Webhooks scheme
    // (HMAC-SHA256 over {id}.{timestamp}.{payload}, emitted as v1,<base64>).
    public WebhookSignatureHeaders Sign(string messageId, DateTimeOffset timestamp, string payload, string secret)
    {
        var unixTimestamp = timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signedContent = $"{messageId}.{unixTimestamp}.{payload}";

        using var hmac = new HMACSHA256(ResolveKey(secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent)));

        return new WebhookSignatureHeaders(messageId, unixTimestamp, $"v1,{signature}");
    }

    // Validates the presence and freshness of the request headers.
    // Returns a failed Result carrying the reason when invalid, otherwise success.
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

    // Verifies the HMAC signature of the payload against the supplied API key.
    public Result VerifySignature(string apiKey, string timestamp, string rawPayload, string signature)
    {
        var expected = ComputeHmacHex(apiKey, $"{timestamp}.{rawPayload}");
        return CryptographicEquals(expected, signature)
            ? Result.Success()
            : Result.Failure("Invalid signature.");
    }

    private static byte[] ResolveKey(string secret)
    {
        if (secret.StartsWith(SecretPrefix, StringComparison.Ordinal) &&
            TryDecodeBase64(secret[SecretPrefix.Length..], out var key))
            return key;

        return Encoding.UTF8.GetBytes(secret);
    }

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        var buffer = new byte[((value.Length + 3) / 4) * 3];
        if (Convert.TryFromBase64String(value, buffer, out var written))
        {
            bytes = buffer[..written];
            return true;
        }

        bytes = [];
        return false;
    }

    private static string ComputeHmacHex(string secret, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).ToLowerInvariant();
    }

    private static bool CryptographicEquals(string a, string b)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
}
