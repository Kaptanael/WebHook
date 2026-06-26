using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common.Options;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEventLifecycleService(
    IWebhookInboundRepository eventRepository,
    IOptions<WebhookDispatchOptions> options,
    ILogger<WebhookEventLifecycleService> logger) : IWebhookEventLifecycleService
{
    private readonly WebhookDispatchOptions _options = options.Value;

    public async Task<bool> MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Status = EventStatus.Processing;
        webhookEvent.ProcessingStartedAtUtc = DateTime.UtcNow;
        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Status = EventStatus.Completed;
        webhookEvent.ProcessedAtUtc = DateTime.UtcNow;
        webhookEvent.NextAttemptAtUtc = null;
        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        var webhookEvent = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (webhookEvent is null)
        {
            return false;
        }

        webhookEvent.Attempts++;
        webhookEvent.LastError = error;

        if (webhookEvent.Attempts >= _options.MaxAttempts)
        {
            webhookEvent.Status = EventStatus.Failed;
            webhookEvent.NextAttemptAtUtc = null;
            logger.LogWarning("Event {EventId} permanently failed after {Attempts} attempt(s). Last error: {Error}", id, webhookEvent.Attempts, error);
        }
        else
        {
            webhookEvent.Status = EventStatus.Retrying;
            webhookEvent.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(NextBackoffSeconds(webhookEvent.Attempts));
            logger.LogInformation("Event {EventId} attempt {Attempts}/{MaxAttempts} failed; retrying at {NextAttempt:O}. Error: {Error}", id, webhookEvent.Attempts, _options.MaxAttempts, webhookEvent.NextAttemptAtUtc, error);
        }

        await eventRepository.UpdateAsync(webhookEvent, cancellationToken);
        return true;
    }

    public async Task<bool> RequeueAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var requeued = await eventRepository.RequeueFailedAsync(id, DateTime.UtcNow, cancellationToken);
        if (requeued)
            logger.LogInformation("Event {EventId} requeued for redelivery from Failed.", id);
        else
            logger.LogWarning("Requeue requested for event {EventId} but no Failed event matched.", id);
        return requeued;
    }

    /// <summary>
    /// Exponential backoff (base * 2^(attempt-1)) capped at the configured ceiling, with "equal jitter":
    /// half the delay is fixed and half is randomized. Keeps a minimum spacing while spreading retries so
    /// a batch failed by a shared outage doesn't all retry at the same instant (thundering herd).
    /// </summary>
    private double NextBackoffSeconds(int attempts)
    {
        var capped = Math.Min(
            _options.RetryBackoffCapSeconds,
            _options.RetryBackoffBaseSeconds * Math.Pow(2, attempts - 1));

        var half = capped / 2.0;
        return half + Random.Shared.NextDouble() * half;
    }
}
