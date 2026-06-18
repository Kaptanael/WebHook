using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class OutboxMessageRepository(IWebhookDbConnectionFactory connectionFactory) : IOutboxMessageRepository
{
    public async Task<Guid> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO OutboxMessages (EventType, Payload, Provider, CompanyId, CreatedAtUtc)
            OUTPUT inserted.Id
            VALUES (@EventType, @Payload, @Provider, @CompanyId, @CreatedAtUtc)
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql, message, cancellationToken: cancellationToken));
        message.Id = id;
        return id;
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, int maxAttempts, CancellationToken cancellationToken = default)
    {
        // UPDLOCK + READPAST: concurrent processors skip locked rows instead of blocking,
        // so each message is handed to exactly one processor instance.
        const string sql = """
            WITH pending AS (
                SELECT TOP (@BatchSize) *
                FROM OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE ProcessedAtUtc IS NULL
                  AND Attempts < @MaxAttempts
                ORDER BY CreatedAtUtc
            )
            UPDATE pending
            SET Attempts = Attempts + 1
            OUTPUT inserted.Id, inserted.EventType, inserted.Payload, inserted.Provider,
                   inserted.CompanyId, inserted.Attempts, inserted.Error, inserted.CreatedAtUtc, inserted.ProcessedAtUtc
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var messages = await connection.QueryAsync<OutboxMessage>(new CommandDefinition(
            sql, new { BatchSize = batchSize, MaxAttempts = maxAttempts }, cancellationToken: cancellationToken));
        return messages.ToList();
    }

    public async Task MarkProcessedAsync(Guid id, DateTime processedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE OutboxMessages SET ProcessedAtUtc = @ProcessedAtUtc WHERE Id = @Id",
            new { Id = id, ProcessedAtUtc = processedAtUtc }, cancellationToken: cancellationToken));
    }

    public async Task MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE OutboxMessages SET Error = @Error WHERE Id = @Id",
            new { Id = id, Error = error }, cancellationToken: cancellationToken));
    }
}
