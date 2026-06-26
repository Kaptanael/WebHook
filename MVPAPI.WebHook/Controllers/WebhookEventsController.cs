using Microsoft.AspNetCore.Mvc;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Controllers;

[ApiController]
[Route("api/webhook/events")]
[Produces("application/json")]
public class WebhookEventsController(
    IWebhookInboundService eventService,
    IWebhookEventLifecycleService eventLifecycle,
    ILogger<WebhookEventsController> logger) : ControllerBase
{
    [NonAction]
    [HttpPost]
    [EndpointSummary("Dispatch events to all subscribed endpoints")]
    [EndpointDescription("Looks up the connection by client token, then queues one pending event per registered endpoint for background delivery.")]
    [ProducesResponseType<IReadOnlyList<WebhookInboundResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish([FromBody] PublishEventRequest request, CancellationToken cancellationToken)
    {
        var events = await eventService.PublishAsync(request, cancellationToken);
        logger.LogInformation("Publish: queued {EventCount} event(s) for event type {EventType}.", events.Count, request.EventType);
        return Ok(events);
    }

    [NonAction]
    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var webhookEvent = await eventService.GetByIdAsync(id, cancellationToken);
        return webhookEvent is null ? NotFound() : Ok(webhookEvent);
    }

    [NonAction]
    [HttpGet("due")]
    public async Task<IActionResult> GetDue([FromQuery] int batchSize = 50, CancellationToken cancellationToken = default)
    {
        var events = await eventService.GetDueForProcessingAsync(batchSize, cancellationToken);
        return Ok(events);
    }

    [NonAction]
    [HttpPost("{id:Guid}/processing")]
    public async Task<IActionResult> MarkProcessing(Guid id, CancellationToken cancellationToken)
    {
        var updated = await eventLifecycle.MarkProcessingAsync(id, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [NonAction]
    [HttpPost("{id:Guid}/completed")]
    public async Task<IActionResult> MarkCompleted(Guid id, CancellationToken cancellationToken)
    {
        var updated = await eventLifecycle.MarkCompletedAsync(id, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [NonAction]
    [HttpPost("{id:Guid}/failed")]
    public async Task<IActionResult> MarkFailed(Guid id, [FromBody] MarkFailedRequest request, CancellationToken cancellationToken)
    {
        var updated = await eventLifecycle.MarkFailedAsync(id, request.Error, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpGet("failed")]
    [EndpointSummary("List dead-letter (permanently failed) events")]
    [EndpointDescription("Returns the most recent events that exhausted all delivery attempts, newest first.")]
    [ProducesResponseType<IReadOnlyList<WebhookInboundResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailed([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 500);
        var events = await eventService.GetFailedAsync(limit, cancellationToken);
        return Ok(events);
    }

    [HttpPost("{id:Guid}/replay")]
    [EndpointSummary("Replay a dead-letter event")]
    [EndpointDescription("Requeues a permanently-failed event for redelivery: resets it to Pending, due now. 404 if no failed event has that id.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Replay(Guid id, CancellationToken cancellationToken)
    {
        var requeued = await eventLifecycle.RequeueAsync(id, cancellationToken);
        if (!requeued)
            return NotFound(new { error = $"No failed event with id {id} to replay." });

        logger.LogInformation("Event {EventId} queued for replay.", id);
        return Accepted(new { replayed = true, id });
    }
}

public record MarkFailedRequest(string Error);
