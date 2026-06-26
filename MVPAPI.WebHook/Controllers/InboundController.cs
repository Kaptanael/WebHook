using Microsoft.AspNetCore.Mvc;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using System.Text;

namespace MVPAPI.WebHook.Controllers;

[ApiController]
[Route("api/inbound")]
[Produces("application/json")]
public class InboundController(IInboundWebhookHandler handler) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Receive an inbound webhook from an external integration")]
    [EndpointDescription("Identifies the integration from the request's credential headers (Standard Webhooks signature, X-Api-Key, or a custom token), authenticates, normalizes the payload to an internal event, and queues it for delivery.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var body = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

        var result = await handler.HandleAsync(new InboundRequest(headers, body), cancellationToken);

        return result.Outcome switch
        {
            InboundOutcome.Accepted => Ok(new { accepted = true, queued = result.QueuedCount }),
            InboundOutcome.Unauthorized => Unauthorized(new { error = result.Error }),
            _ => BadRequest(new { error = result.Error }),
        };
    }
}
