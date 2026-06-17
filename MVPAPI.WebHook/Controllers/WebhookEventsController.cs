using Microsoft.AspNetCore.Mvc;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Controllers;

[ApiController]
[Route("api/webhook/events")]
public class WebhookEventsController(IWebhookEventService eventService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishEventRequest request, CancellationToken cancellationToken)
    {
        var events = await eventService.PublishAsync(request, cancellationToken);
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
        var updated = await eventService.MarkProcessingAsync(id, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [NonAction]
    [HttpPost("{id:Guid}/completed")]
    public async Task<IActionResult> MarkCompleted(Guid id, CancellationToken cancellationToken)
    {
        var updated = await eventService.MarkCompletedAsync(id, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [NonAction]
    [HttpPost("{id:Guid}/failed")]
    public async Task<IActionResult> MarkFailed(Guid id, [FromBody] MarkFailedRequest request, CancellationToken cancellationToken)
    {
        var updated = await eventService.MarkFailedAsync(id, request.Error, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}

public record MarkFailedRequest(string Error);
