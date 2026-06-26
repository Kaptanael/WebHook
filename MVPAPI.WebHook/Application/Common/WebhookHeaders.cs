namespace MVPAPI.WebHook.Application.Common;

public static class WebhookHeaders
{
    public const string ApiKeyHeader = "x-api-key";
    public const string TokenHeader = "x-token";
    public const string IdHeader = "webhook-id";
    public const string TimestampHeader = "webhook-timestamp";
    public const string SignatureHeader = "webhook-signature";
}
