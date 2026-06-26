using Microsoft.AspNetCore.Mvc;
using MVPAPI.WebHook.Application.DTOs.Connections;
using MVPAPI.WebHook.Application.DTOs.Outbounds;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WebHookController(
    IWebHookConnectionService connectionService,
    IWebhookEndpointService endpointService,    
    ILogger<WebHookController> logger) : ControllerBase
{
    [HttpPost("connect")]
    [EndpointSummary("Establish a webhook connection")]
    [EndpointDescription("Validates the client token against the MVP API and creates or refreshes the connection record.")]
    [ProducesResponseType<CreateConnectionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Connect([FromHeader] string token, CancellationToken cancellationToken = default)
    {
        var result = await connectionService.Connect(token, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Webhook connect failed: {Error}", result.Error);
            return Unauthorized(new { Success = false, Error = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpPost("subscribe")]
    [EndpointSummary("Subscribe to webhook events")]
    [EndpointDescription("Registers a callback endpoint to receive webhook events for the company associated with the token.")]
    [ProducesResponseType<SubscribeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromHeader] string token, [FromBody] SubscribeRequest request, CancellationToken cancellationToken)
    {
        var result = await endpointService.SubscribeAsync(token, request, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Webhook subscribe failed: {Error}", result.Error);
            return BadRequest(new { Success = false, Error = result.Error });
        }

        var subscription = result.Value!;
        subscription.ResponseSchemaUrl = Url.Action(
            action: nameof(GetSchema),
            controller: "WebHook",
            values: new { id = subscription.Id },
            protocol: Request.Scheme
        ) ?? string.Empty;

        logger.LogInformation("Subscriber {SubscriberId} subscribed for {Endpoint}.", subscription.Id, request.Endpoint);
        return Ok(result.Value);
    }

    [HttpDelete("unsubscribe/{id}")]
    [EndpointSummary("Unsubscribe from webhook events")]
    [EndpointDescription("Removes the registered callback endpoint identified by the given subscription ID.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsubscribe([FromHeader] string token, Guid id, CancellationToken cancellationToken)
    {
        var result = await endpointService.UnsubscribeAsync(token, id, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Webhook unsubscribe failed: {Error}", result.Error);
            return NotFound(new { message = result.Error });
        }

        logger.LogInformation("Unsubscribe: endpoint {Id} removed successfully.", id);
        return Ok(new { message = "Unsubscribed successfully." });
    }

    [HttpGet("{id:guid}/schema")]
    [EndpointSummary("Get the event schema for a subscription")]
    [EndpointDescription("Returns the trigger definition schema and a sample payload for the given subscription ID.")]
    [ProducesResponseType<WebhookSchemaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchema(Guid id)
    {
        var result = await endpointService.GetSchemaAsync(id);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Webhook schema failed: {Error}", result.Error);
            return NotFound(new { message = result.Error });
        }

        var schema = result.Value!;
        logger.LogInformation("Webhook schema returned successfully for WebhookId {WebhookId}, Endpoint {Endpoint}.", schema.Id, schema.Endpoint);
        return Ok(result.Value);
    }    
}
