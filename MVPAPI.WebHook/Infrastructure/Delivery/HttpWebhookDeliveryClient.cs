using System.Text;
using Microsoft.Extensions.Logging;
using MVPAPI.WebHook.Application.Interfaces;

namespace MVPAPI.WebHook.Infrastructure.Delivery;

public class HttpWebhookDeliveryClient(
    HttpClient httpClient,
    ILogger<HttpWebhookDeliveryClient> logger) : IWebhookDeliveryClient
{
    public async Task<DeliveryResult> DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.TargetUrl)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Webhook-Event", delivery.EventType);
            request.Headers.Add("X-Webhook-Token", delivery.EndpointToken);
            request.Headers.Add("X-Webhook-Delivery-Id", delivery.EventId.ToString());

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return DeliveryResult.Ok();
            }

            return DeliveryResult.Fail($"Target responded with {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Delivery of event {EventId} to {TargetUrl} failed.", delivery.EventId, delivery.TargetUrl);
            return DeliveryResult.Fail($"HTTP request failed: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeliveryResult.Fail("Delivery timed out.");
        }
        catch (UriFormatException)
        {
            return DeliveryResult.Fail($"Target URL is not valid: {delivery.TargetUrl}");
        }
    }
}
