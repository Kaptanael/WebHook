using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MVPAPI.WebHook.Infrastructure.Delivery;

public class HttpWebhookDeliveryClient(
    HttpClient httpClient,
    ITokenDecoder tokenDecoder,
    ILogger<HttpWebhookDeliveryClient> logger) : IWebhookDeliveryClient
{
    public async Task<DeliveryResult> DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenDecoderResult = tokenDecoder.Decode(delivery.EndpointToken);
            if (!tokenDecoderResult.IsSuccess)
            {
                return DeliveryResult.Fail($"Failed to decode endpoint token: {tokenDecoderResult.Error}");
            }

            var apiKey = tokenDecoderResult.Value!.ApiKey;

            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.TargetUrl)
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
