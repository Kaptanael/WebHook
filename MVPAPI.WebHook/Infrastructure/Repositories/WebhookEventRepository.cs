using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class WebhookEventRepository(IDbConnectionFactory connectionFactory) : IWebhookEventRepository
{
    private const string SelectColumns = """
        SELECT Id, WebhookId, Provider, EventType, Payload, Status, Attempts, LastError,
               ReceivedAtUtc, NextAttemptAtUtc, ProcessingStartedAtUtc, ProcessedAtUtc
        FROM WebhookEvents
        """;

    public async Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookEvent>(new CommandDefinition(
            $"{SelectColumns} WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<WebhookEvent>> GetByEndpointIdAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookEvent>(new CommandDefinition(
            $"{SelectColumns} WHERE WebhookId = @WebhookId", new { WebhookId = webhookId }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<IReadOnlyList<WebhookEvent>> GetDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@BatchSize) Id, WebhookId, Provider, EventType, Payload, Status, Attempts, LastError,
                   ReceivedAtUtc, NextAttemptAtUtc, ProcessingStartedAtUtc, ProcessedAtUtc
            FROM WebhookEvents
            WHERE Status IN (@Pending, @Retrying)
              AND NextAttemptAtUtc IS NOT NULL
              AND NextAttemptAtUtc <= @NowUtc
            ORDER BY NextAttemptAtUtc
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookEvent>(new CommandDefinition(sql, new
        {
            BatchSize = batchSize,
            Pending = (byte)EventStatus.Pending,
            Retrying = (byte)EventStatus.Retrying,
            NowUtc = nowUtc
        }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<IReadOnlyList<WebhookEvent>> ClaimDueForProcessingAsync(int batchSize, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        // UPDLOCK + READPAST: concurrent claimers skip rows locked by another instance
        // instead of blocking on them, so each event is handed to exactly one claimer.
        const string sql = """
            WITH due AS (
                SELECT TOP (@BatchSize) *
                FROM WebhookEvents WITH (UPDLOCK, READPAST, ROWLOCK)
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
        var events = await connection.QueryAsync<WebhookEvent>(new CommandDefinition(sql, new
        {
            BatchSize = batchSize,
            Pending = (byte)EventStatus.Pending,
            Retrying = (byte)EventStatus.Retrying,
            Processing = (byte)EventStatus.Processing,
            NowUtc = nowUtc
        }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<IReadOnlyList<WebhookEvent>> ClaimStaleProcessingAsync(DateTime cutoffUtc, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE e
            SET ProcessingStartedAtUtc = @NowUtc
            OUTPUT inserted.Id, inserted.WebhookId, inserted.Provider, inserted.EventType, inserted.Payload,
                   inserted.Status, inserted.Attempts, inserted.LastError, inserted.ReceivedAtUtc,
                   inserted.NextAttemptAtUtc, inserted.ProcessingStartedAtUtc, inserted.ProcessedAtUtc
            FROM WebhookEvents e WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE e.Status = @Processing
              AND e.ProcessingStartedAtUtc IS NOT NULL
              AND e.ProcessingStartedAtUtc <= @CutoffUtc
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var events = await connection.QueryAsync<WebhookEvent>(new CommandDefinition(sql, new
        {
            Processing = (byte)EventStatus.Processing,
            CutoffUtc = cutoffUtc,
            NowUtc = nowUtc
        }, cancellationToken: cancellationToken));
        return events.ToList();
    }

    public async Task<Guid> AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO WebhookEvents
                (WebhookId, Provider, EventType, Payload, Status, Attempts, LastError, ReceivedAtUtc, NextAttemptAtUtc)
            OUTPUT inserted.Id
            VALUES
                (@WebhookId, @Provider, @EventType, @Payload, @Status, @Attempts, @LastError, @ReceivedAtUtc, @NextAttemptAtUtc)
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql, webhookEvent, cancellationToken: cancellationToken));
        webhookEvent.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE WebhookEvents
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
