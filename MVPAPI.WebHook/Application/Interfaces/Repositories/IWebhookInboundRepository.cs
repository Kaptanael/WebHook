using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IWebhookInboundRepository
{
    Task<WebhookInbound?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WebhookInbound?> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebhookInbound>> GetDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Most recent events in the given status (e.g. Failed, for the dead-letter view).</summary>
    Task<IReadOnlyList<WebhookInbound>> GetByStatusAsync(EventStatus status, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a permanently-Failed event back to Pending (attempts cleared, due now) so the dispatcher
    /// redelivers it. No-op for events that aren't Failed. Returns true when a row was requeued.
    /// </summary>
    Task<bool> RequeueFailedAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> due events by marking them Processing.
    /// Safe to call concurrently from multiple instances: each event is claimed by exactly one caller.
    /// </summary>
    Task<IReadOnlyList<WebhookInbound>> ClaimDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims events stuck in Processing since before <paramref name="cutoffUtc"/> by
    /// refreshing their claim timestamp, so only one instance recovers each stale event.
    /// </summary>
    Task<IReadOnlyList<WebhookInbound>> ClaimStaleProcessingAsync(DateTime cutoffUtc, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Inserts the event and populates its Id.</summary>
    Task<Guid> AddAsync(WebhookInbound webhookEvent, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(WebhookInbound webhookEvent, CancellationToken cancellationToken = default);

}
