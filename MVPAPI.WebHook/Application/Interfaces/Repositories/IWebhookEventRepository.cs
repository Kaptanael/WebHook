using Microsoft.AspNetCore.DataProtection.KeyManagement;
using MVPAPI.WebHook.Domain.Entities;
using System.Threading;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IWebhookEventRepository
{
    Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookEvent>> GetDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> due events by marking them Processing.
    /// Safe to call concurrently from multiple instances: each event is claimed by exactly one caller.
    /// </summary>
    Task<IReadOnlyList<WebhookEvent>> ClaimDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims events stuck in Processing since before <paramref name="cutoffUtc"/> by
    /// refreshing their claim timestamp, so only one instance recovers each stale event.
    /// </summary>
    Task<IReadOnlyList<WebhookEvent>> ClaimStaleProcessingAsync(DateTime cutoffUtc, DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Inserts the event and populates its Id.</summary>
    Task<Guid> AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);

}
