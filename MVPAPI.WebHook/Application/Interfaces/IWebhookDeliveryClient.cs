namespace MVPAPI.WebHook.Application.Interfaces;

public record WebhookDelivery(
    Guid EventId,
    string TargetUrl,
    string EventType,
    string Payload,
    string EndpointToken);

public record DeliveryResult(bool Success, string? Error)
{
    public static DeliveryResult Ok() => new(true, null);
    public static DeliveryResult Fail(string error) => new(false, error);
}

public interface IWebhookDeliveryClient
{
    Task<DeliveryResult> DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
}
