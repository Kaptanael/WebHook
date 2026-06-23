namespace MVPAPI.WebHook.Application.Interfaces.Services;

/// <summary>
/// The Standard Webhooks signing headers for an outbound request:
/// <c>webhook-id</c>, <c>webhook-timestamp</c>, and <c>webhook-signature</c>.
/// </summary>
public record WebhookSignatureHeaders(string Id, string Timestamp, string Signature);

public interface IWebhookSigner
{
    /// <summary>
    /// Signs an outbound webhook payload using the Standard Webhooks scheme
    /// (HMAC-SHA256 over <c>{id}.{timestamp}.{payload}</c>, emitted as <c>v1,&lt;base64&gt;</c>).
    /// </summary>
    /// <param name="messageId">Stable, unique id for the message (the event id), echoed in <c>webhook-id</c>.</param>
    /// <param name="timestamp">Send time; serialized to a Unix-seconds <c>webhook-timestamp</c>.</param>
    /// <param name="payload">Raw request body that is signed.</param>
    /// <param name="secret">Shared secret known to both sender and receiver (never transmitted on the wire).</param>
    WebhookSignatureHeaders Sign(string messageId, DateTimeOffset timestamp, string payload, string secret);
}
