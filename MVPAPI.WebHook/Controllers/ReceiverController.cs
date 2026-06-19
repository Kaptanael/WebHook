using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MVPAPI.WebHook.Controllers;

[ApiController]
[Route("api/receiver")]
[Produces("application/json")]
public class ReceiverController(ILogger<ReceiverController> logger) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Receive a webhook delivery (test sink)")]
    [EndpointDescription("Accepts an inbound webhook POST from the dispatcher, logs the headers and payload, and returns 200. Intended as a local sink for end-to-end testing webhook delivery.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Receive([FromBody] JsonElement payload)
    {
        // Log only the presence of credentials, never their values, to avoid leaking secrets into logs.
        var apiKeyPresent = Request.Headers.TryGetValue("api-key", out var apiKey) && !string.IsNullOrWhiteSpace(apiKey);
        var bearerPresent = Request.Headers.Authorization.ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        var rawPayload = payload.ValueKind == JsonValueKind.Undefined ? string.Empty : payload.GetRawText();

        logger.LogInformation(
            "Receiver accepted webhook delivery: apiKeyPresent={ApiKeyPresent}, bearerPresent={BearerPresent}, payload={Payload}",
            apiKeyPresent, bearerPresent, rawPayload);

        return Ok(new { received = true, receivedAtUtc = DateTime.UtcNow });
    }
}
