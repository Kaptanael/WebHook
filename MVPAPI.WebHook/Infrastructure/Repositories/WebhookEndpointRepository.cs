using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class WebhookEndpointRepository(IDbConnectionFactory connectionFactory) : IWebhookEndpointRepository
{
    private const string SelectColumns = """
        SELECT Id, EndPointToken, Endpoint, CompanyId, TriggerConfigJson, IsActive, CreatedAtUtc, ActionDataSchema
        FROM WebhookEndpoints
        """;

    public async Task<WebhookEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookEndpoint>(new CommandDefinition(
            $"{SelectColumns} WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<WebhookEndpoint?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookEndpoint>(new CommandDefinition(
            $"{SelectColumns} WHERE Endpoint = @Endpoint", new { Endpoint = endpoint }, cancellationToken: cancellationToken));
    }

    public async Task<WebhookEndpoint?> GetByEndpointTokenAsync(string endpointToken, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebhookEndpoint>(new CommandDefinition(
            $"{SelectColumns} WHERE EndPointToken = @EndPointToken", new { EndPointToken = endpointToken }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<WebhookEndpoint>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var endpoints = await connection.QueryAsync<WebhookEndpoint>(new CommandDefinition(
            $"{SelectColumns} WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, cancellationToken: cancellationToken));
        return endpoints.ToList();
    }

    public async Task<Guid> AddAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO WebhookEndpoints
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

    public async Task<bool> UpdateAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE WebhookEndpoints
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

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM WebhookEndpoints WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
        return affected > 0;
    }    
}
