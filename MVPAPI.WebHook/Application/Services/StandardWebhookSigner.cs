using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Application.Services;

/// <summary>
/// Signs outbound deliveries with the Standard Webhooks scheme (https://www.standardwebhooks.com/).
/// The signed content is <c>{id}.{timestamp}.{payload}</c>, HMAC-SHA256 keyed with the endpoint's
/// shared secret and emitted as the versioned <c>v1,&lt;base64&gt;</c> form.
/// <para>
/// A <c>whsec_</c>-prefixed secret is treated as the canonical Standard Webhooks form: the prefix is
/// stripped and the remainder base64-decoded to obtain the key bytes, so off-the-shelf third-party
/// verifier libraries interoperate. Any other secret is keyed as raw UTF-8 bytes, matching the
/// convention used by <see cref="WebhookSignatureVerifier"/>.
/// </para>
/// </summary>
public class StandardWebhookSigner : IWebhookSigner
{
    private const string SecretPrefix = "whsec_";

    public WebhookSignatureHeaders Sign(string messageId, DateTimeOffset timestamp, string payload, string secret)
    {
        var unixTimestamp = timestamp.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var signedContent = $"{messageId}.{unixTimestamp}.{payload}";

        using var hmac = new HMACSHA256(ResolveKey(secret));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent)));

        return new WebhookSignatureHeaders(messageId, unixTimestamp, $"v1,{signature}");
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
}
