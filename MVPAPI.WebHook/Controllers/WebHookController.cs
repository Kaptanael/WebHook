using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Endpoints;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Services;

namespace MVPAPI.WebHook.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebHookController(
    IWebHookConnectionService connectionService,
    IWebhookEndpointService endpointService,
    ILogger<WebHookController> logger) : ControllerBase
{
    [HttpPost("connect")]
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
    public async Task<IActionResult> GetSchema(Guid id)
    {
        var result = await endpointService.GetSchemaAsync(id);
        if (!result.IsSuccess)
        {
            logger.LogWarning("Webhook schema failed: {Error}", result.Error);
            return NotFound(new { message = result.Error });
        }

        var schema = result.Value!;
        logger.LogInformation("Webhook schema returned successfully for WebhookId {WebhookId}, Endpoint {Endpoint}.", schema.Id,schema.Endpoint);
        return Ok(result.Value);
    }

    [HttpGet("trigger-definitions")]
    public IActionResult GetTriggerDefinitions()
    {
        var definitions = WebhookTriggerDefinitionProvider.GetAll();

        return Ok(definitions);
    }
}
