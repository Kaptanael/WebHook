using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class WebHookConnectionRepository(IWebhookDbConnectionFactory connectionFactory) : IWebHookConnectionRepository
{
    private const string SelectColumns = """
        SELECT Id, CompanyId, ApplicationName, ClientToken, IsActive,
               MVPApiToken, MVPApiRefreshToken, MVPApiExpiresIn, MVPAuthKeyJson, SigningSecret, CreatedAtUtc
        FROM WebHookConnection
        """;

    public async Task<WebHookConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebHookConnection>(new CommandDefinition(
            $"{SelectColumns} WHERE Id = @Id", new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<WebHookConnection?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebHookConnection>(new CommandDefinition(
            $"{SelectColumns} WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, cancellationToken: cancellationToken));
    }

    public async Task<WebHookConnection?> GetByClientTokenAsync(string clientToken, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<WebHookConnection>(new CommandDefinition(
            $"{SelectColumns} WHERE ClientToken = @ClientToken", new { ClientToken = clientToken }, cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM WebHookConnection WHERE CompanyId = @CompanyId) THEN 1 ELSE 0 END",
            new { CompanyId = companyId }, cancellationToken: cancellationToken));
    }

    public async Task<Guid> AddAsync(WebHookConnection webHookConnection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO WebHookConnection
                (CompanyId, ApplicationName, ClientToken, MVPApiToken, MVPApiRefreshToken, MVPApiExpiresIn, MVPAuthKeyJson)
            OUTPUT inserted.Id
            VALUES
                (@CompanyId, @ApplicationName, @ClientToken, @MVPApiToken, @MVPApiRefreshToken, @MVPApiExpiresIn, @MVPAuthKeyJson)
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql, webHookConnection, cancellationToken: cancellationToken));
        webHookConnection.Id = id;
        return id;
    }

    public async Task<bool> UpdateAsync(WebHookConnection webHookConnection, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE WebHookConnection
            SET ApplicationName    = @ApplicationName,
                ClientToken        = @ClientToken,
                IsActive           = @IsActive,
                MVPApiToken        = @MVPApiToken,
                MVPApiRefreshToken = @MVPApiRefreshToken,
                MVPApiExpiresIn    = @MVPApiExpiresIn,
                MVPAuthKeyJson     = @MVPAuthKeyJson
            WHERE Id = @Id
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql, webHookConnection, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
