using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class ApiKeyRepository(IPortalDbConnectionFactory connectionFactory) : IApiKeyRepository
{
    // Status is a tinyint flag (1 = active); cast to text so ApiKey.IsActive can match it uniformly.
    // CompanyId is a bigint; narrow to int to match the WebhookEndpoint.CompanyId it is joined on.
    private const string Sql = """
        SELECT TOP (1) Id, RawApiKey, Salt, CAST(CompanyId AS INT) AS CompanyId, CAST(Status AS NVARCHAR(50)) AS Status
        FROM [PortalDB].[dbo].[ApiKeys]
        WHERE RawApiKey = @RawApiKey
        """;

    public async Task<ApiKey?> GetByRawApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ApiKey>(new CommandDefinition(
            Sql, new { RawApiKey = rawApiKey }, cancellationToken: cancellationToken));
    }
}
