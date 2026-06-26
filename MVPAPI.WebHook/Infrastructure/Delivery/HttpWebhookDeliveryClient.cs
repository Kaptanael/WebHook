using Microsoft.Data.SqlClient.Internal;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Options;
using MVPAPI.WebHook.Application.Interfaces.Services;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MVPAPI.WebHook.Infrastructure.Delivery;

public class HttpWebhookDeliveryClient(
    HttpClient httpClient,
    IOptions<WebhookRouteOptions> routeOptions,
    ILogger<HttpWebhookDeliveryClient> logger) : IWebhookDeliveryClient
{
    public async Task<DeliveryResult> DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        if (!routeOptions.Value.Routes.TryGetValue(delivery.EventType, out var deliveryUrl) || string.IsNullOrWhiteSpace(deliveryUrl))
        {
            logger.LogWarning("Cannot provision endpoint for company {CompanyId}: no WebhookRoutes entry for event type '{EventType}'.", delivery.EventId, delivery.EventType);
            return DeliveryResult.Fail($"No WebhookRoutes entry for event type '{delivery.EventType}'.");
        }

        try
        {
            var tokenDecoderResult = ClientTokenConverter.Decode(delivery.EndpointToken);
            if (!tokenDecoderResult.IsSuccess)
            {
                logger.LogWarning("Failed to decode endpoint token for event {EventId}: {Error}", delivery.EventId, tokenDecoderResult.Error);
                return DeliveryResult.Fail($"Failed to decode endpoint token: {tokenDecoderResult.Error}");
            }

            var apiKey = tokenDecoderResult.Value!.ApiKey;            

            using var request = new HttpRequestMessage(HttpMethod.Post, deliveryUrl)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
            };            

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", delivery.MVPApiToken);
            request.Headers.Add("api-key", apiKey);            

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return DeliveryResult.Ok();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return DeliveryResult.Unauthorized();

            return DeliveryResult.Fail($"Target responded with {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Delivery of event {EventId} to {TargetUrl} failed.", delivery.EventId, deliveryUrl);
            return DeliveryResult.Fail($"HTTP request failed: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeliveryResult.Fail("Delivery timed out.");
        }
        catch (UriFormatException)
        {
            return DeliveryResult.Fail($"Target URL is not valid: {deliveryUrl}");
        }
    }
}
