using System.Security.Cryptography;

namespace MVPAPI.WebHook.Application.Common;

/// <summary>
/// Generates per-endpoint Standard Webhooks signing secrets in the canonical
/// <c>whsec_&lt;base64&gt;</c> form (24 random bytes), so third-party verifier libraries can
/// validate outbound signatures using the same secret.
/// </summary>
public static class WebhookSigningSecret
{
    public const string Prefix = "whsec_";

    // Standard Webhooks' default symmetric key size.
    private const int SecretByteLength = 24;

    public static string Generate() =>
        Prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretByteLength));
}
