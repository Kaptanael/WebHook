using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;

namespace MVPAPI.WebHook.Infrastructure.Delivery;

public class HttpWebhookResponseForwarder(
    HttpClient httpClient,
    IWebhookSigner signer,
    ILogger<HttpWebhookResponseForwarder> logger) : IWebhookResponseForwarder
{
    public async Task<DeliveryResult> ForwardAsync(WebhookResponseForward forward, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, forward.CallbackUrl)
            {
                Content = new StringContent(forward.ResponseBody, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", forward.MVPApiToken);
            if (!string.IsNullOrWhiteSpace(forward.ApiKey))
                request.Headers.Add("api-key", forward.ApiKey);

            // Standard Webhooks signature over the forwarded body, keyed with the endpoint's
            // ClientSecret so the callback recipient can verify it independently of the auth headers.
            var signature = signer.Sign(forward.EventId.ToString(), DateTimeOffset.UtcNow, forward.ResponseBody, forward.SigningSecret);
            request.Headers.Add("webhook-id", signature.Id);
            request.Headers.Add("webhook-timestamp", signature.Timestamp);
            request.Headers.Add("webhook-signature", signature.Signature);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
                return DeliveryResult.Ok();

            return DeliveryResult.Fail($"Callback responded with {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Forwarding response for event {EventId} to {CallbackUrl} failed.", forward.EventId, forward.CallbackUrl);
            return DeliveryResult.Fail($"Callback request failed: {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DeliveryResult.Fail("Callback timed out.");
        }
        catch (UriFormatException)
        {
            return DeliveryResult.Fail($"Callback URL is not valid: {forward.CallbackUrl}");
        }
    }
}
