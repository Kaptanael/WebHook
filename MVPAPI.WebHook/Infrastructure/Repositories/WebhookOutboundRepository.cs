using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class WebhookOutboundRepository(IWebhookDbConnectionFactory connectionFactory) : IWebhookOutboundRepository
{
    private const string SelectColumns = """
        SELECT Id, EndPointToken, Endpoint, CompanyId, TriggerConfigJson, IsActive, CreatedAtUtc, ActionDataSchema
        FROM WebhookOutbound
        """;

    public async Task<IReadOnlyList<WebhookOutbound>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var endpoints = await connection.QueryAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE IsActive = 1", cancellationToken: cancellationToken));
        return endpoints.ToList();
    }

    public async Task<WebhookOutbound?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<WebhookOutbound?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE Endpoint = @Endpoint", new { Endpoint = endpoint }, cancellationToken: cancellationToken));
    }

    public async Task<WebhookOutbound?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE IsActive = 1 AND EndPointToken = @Token", new { Token = token }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<WebhookOutbound>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var endpoints = await connection.QueryAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, cancellationToken: cancellationToken));
        return endpoints.ToList();
    }

    public async Task<Guid> AddAsync(WebhookOutbound endpoint, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO WebhookOutbound
                (EndPointToken, Endpoint, CompanyId, TriggerConfigJson, ActionDataSchema)
            OUTPUT inserted.Id
            VALUES
                (@EndPointToken, @Endpoint, @CompanyId, @TriggerConfigJson, @ActionDataSchema)
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql, endpoint, cancellationToken: cancellationToken));
        endpoint.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(WebhookOutbound endpoint, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE WebhookOutbound
            SET EndPointToken       = @EndPointToken,
                Endpoint            = @Endpoint,
                CompanyId           = @CompanyId,
                TriggerJson         = @TriggerJson,
                IsActive            = @IsActive,
                ActionDataSchema    = @ActionDataSchema
            WHERE Id = @Id
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, endpoint, cancellationToken: cancellationToken));
        return affected > 0;
    }

    public async Task<IReadOnlyList<WebhookOutbound>> GetActiveByCompanyAndEventTypeAsync(int companyId, string eventType, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var endpoints = await connection.QueryAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE IsActive = 1 AND CompanyId = @CompanyId AND JSON_VALUE(TriggerConfigJson, '$.triggerType') = @EventType",
            new { CompanyId = companyId, EventType = eventType }, cancellationToken: cancellationToken));
        return endpoints.ToList();
    }

    public async Task<WebhookOutbound?> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var endpoints = await connection.QueryAsync<WebhookOutbound>(new CommandDefinition(
            $"{SelectColumns} WHERE IsActive = 1 AND EventType = @EventType",
            new { EventType = eventType }, cancellationToken: cancellationToken));
        return endpoints.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM WebhookOutbound WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
        return affected > 0;
    }    
}
