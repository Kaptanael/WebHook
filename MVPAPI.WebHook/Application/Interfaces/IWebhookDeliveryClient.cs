namespace MVPAPI.WebHook.Application.Interfaces;

public record WebhookDelivery(
    Guid EventId,
    string TargetUrl,
    string EventType,
    string Payload,
    string EndpointToken,
    string MVPApiToken,
    string MVPApiRefreshToken);

public record DeliveryResult(bool Success, string? Error, bool IsUnauthorized = false)
{
    public static DeliveryResult Ok() => new(true, null);
    public static DeliveryResult Fail(string error) => new(false, error);
    public static DeliveryResult Unauthorized() => new(false, "Bearer token expired.", IsUnauthorized: true);
}

public interface IWebhookDeliveryClient
{
    Task<DeliveryResult> DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default);
}
