namespace MVPAPI.WebHook.Application.Interfaces;

public record WebhookResponseForward(
    Guid EventId,
    string CallbackUrl,
    string ResponseBody,
    string MVPApiToken,
    string ApiKey,
    string SigningSecret);

public interface IWebhookResponseForwarder
{
    /// <summary>
    /// Forwards a subscriber's delivery response to the callback URL.
    /// </summary>
    Task<DeliveryResult> ForwardAsync(WebhookResponseForward forward, CancellationToken cancellationToken = default);
}
