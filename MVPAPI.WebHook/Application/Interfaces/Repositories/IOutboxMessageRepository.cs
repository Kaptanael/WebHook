using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IOutboxMessageRepository
{
    Task<Guid> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> unprocessed messages whose attempt
    /// count is below <paramref name="maxAttempts"/>, incrementing Attempts on each claimed row.
    /// Safe to call concurrently: UPDLOCK + READPAST ensures each message is claimed once.
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid id, DateTime processedAtUtc, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
}
