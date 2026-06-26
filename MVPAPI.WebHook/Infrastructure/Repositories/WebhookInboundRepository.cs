using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class WebhookInboundRepository(IWebhookDbConnectionFactory connectionFactory) : IWebhookInboundRepository
{
    private const string SelectColumns = """
        SELECT Id, WebhookId, Provider, EventType, Payload, Status, Attempts, LastError,
               ReceivedAtUtc, NextAttemptAtUtc, ProcessingStartedAtUtc, ProcessedAtUtc, IdempotencyKey
        FROM WebhookInbound
        """;

    public async Task<WebhookInbound?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookInbound>(new CommandDefinition(
            $"{SelectColumns} WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<WebhookInbound?> GetByEventTypeAsync(string eventType, CancellationToken cancellationToken = default) 
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<WebhookInbound>(new CommandDefinition(
            $"{SelectColumns} WHERE EventType = @EventType", new { EventType = eventType }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<WebhookInbound>> GetByStatusAsync(EventStatus status, int limit, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT TOP (@Limit) Id, WebhookId, Provider, EventType, Payload, Status, Attempts, LastError,
                   ReceivedAtUtc, NextAttemptAtUtc, ProcessingStartedAtUtc, ProcessedAtUtc, IdempotencyKey
            FROM WebhookInbound
            WHERE Status = @Status
            ORDER BY ReceivedAtUtc DESC
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookInbound>(new CommandDefinition(
            sql, new { Limit = limit, Status = (byte)status }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<bool> RequeueFailedAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // Only Failed events are requeued; LastError is kept for history. NextAttemptAtUtc is set to now
        // so the dispatcher (which requires NextAttemptAtUtc <= now) picks it up on the next poll.
        const string sql = """
            UPDATE WebhookInbound
            SET Status                 = @Pending,
                Attempts               = 0,
                NextAttemptAtUtc       = @NowUtc,
                ProcessingStartedAtUtc = NULL,
                ProcessedAtUtc         = NULL
            WHERE Id = @Id AND Status = @Failed
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            NowUtc = nowUtc,
            Pending = (byte)EventStatus.Pending,
            Failed = (byte)EventStatus.Failed
        }, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<IReadOnlyList<WebhookInbound>> GetDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@BatchSize) Id, WebhookId, Provider, EventType, Payload, Status, Attempts, LastError,
                   ReceivedAtUtc, NextAttemptAtUtc, ProcessingStartedAtUtc, ProcessedAtUtc
            FROM WebhookInbound
            WHERE Status IN (@Pending, @Retrying)
              AND NextAttemptAtUtc IS NOT NULL
              AND NextAttemptAtUtc <= @NowUtc
            ORDER BY NextAttemptAtUtc
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookInbound>(new CommandDefinition(sql, new
        {
            BatchSize = batchSize,
            Pending = (byte)EventStatus.Pending,
            Retrying = (byte)EventStatus.Retrying,
            NowUtc = nowUtc
        }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<IReadOnlyList<WebhookInbound>> ClaimDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // UPDLOCK + READPAST: concurrent claimers skip rows locked by another instance
        // instead of blocking on them, so each event is handed to exactly one claimer.
        const string sql = """
            WITH due AS (
                SELECT TOP (@BatchSize) *
                FROM WebhookInbound WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE Status IN (@Pending, @Retrying)
                  AND NextAttemptAtUtc IS NOT NULL
                  AND NextAttemptAtUtc <= @NowUtc
                ORDER BY NextAttemptAtUtc
            )
            UPDATE due
            SET Status = @Processing,
                ProcessingStartedAtUtc = @NowUtc
            OUTPUT inserted.Id, inserted.WebhookId, inserted.Provider, inserted.EventType, inserted.Payload,
                   inserted.Status, inserted.Attempts, inserted.LastError, inserted.ReceivedAtUtc,
                   inserted.NextAttemptAtUtc, inserted.ProcessingStartedAtUtc, inserted.ProcessedAtUtc;
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookInbound>(new CommandDefinition(sql, new
        {
            BatchSize = batchSize,
            Pending = (byte)EventStatus.Pending,
            Retrying = (byte)EventStatus.Retrying,
            Processing = (byte)EventStatus.Processing,
            NowUtc = nowUtc
        }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<IReadOnlyList<WebhookInbound>> ClaimStaleProcessingAsync(DateTime cutoffUtc, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE e
            SET ProcessingStartedAtUtc = @NowUtc
            OUTPUT inserted.Id, inserted.WebhookId, inserted.Provider, inserted.EventType, inserted.Payload,
                   inserted.Status, inserted.Attempts, inserted.LastError, inserted.ReceivedAtUtc,
                   inserted.NextAttemptAtUtc, inserted.ProcessingStartedAtUtc, inserted.ProcessedAtUtc
            FROM WebhookInbound e WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE e.Status = @Processing
              AND e.ProcessingStartedAtUtc IS NOT NULL
              AND e.ProcessingStartedAtUtc <= @CutoffUtc
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookInbound>(new CommandDefinition(sql, new
        {
            Processing = (byte)EventStatus.Processing,
            CutoffUtc = cutoffUtc,
            NowUtc = nowUtc
        }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<Guid> AddAsync(WebhookInbound webhookEvent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO WebhookInbound
                (WebhookId, Provider, EventType, Payload, Status, Attempts, LastError, ReceivedAtUtc, NextAttemptAtUtc, IdempotencyKey)
            OUTPUT inserted.Id
            VALUES
                (@WebhookId, @Provider, @EventType, @Payload, @Status, @Attempts, @LastError, @ReceivedAtUtc, @NextAttemptAtUtc, @IdempotencyKey)
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql, webhookEvent, cancellationToken: cancellationToken));
        webhookEvent.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(WebhookInbound webhookEvent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE WebhookInbound
            SET Status                 = @Status,
                Attempts               = @Attempts,
                LastError              = @LastError,
                NextAttemptAtUtc       = @NextAttemptAtUtc,
                ProcessingStartedAtUtc = @ProcessingStartedAtUtc,
                ProcessedAtUtc         = @ProcessedAtUtc
            WHERE Id = @Id
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, webhookEvent, cancellationToken: cancellationToken));
        return affected > 0;
    }    
}
