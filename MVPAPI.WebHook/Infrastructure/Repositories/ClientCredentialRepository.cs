using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class ClientCredentialRepository(IPortalDbConnectionFactory connectionFactory) : IClientCredentialRepository
{
    // CompanyId is a bigint in PortalDB; narrow to int to match the rest of the app.
    private const string Sql = """
        SELECT TOP (1) Id, CAST(CompanyId AS INT) AS CompanyId, ClientId, Secret, IsActive, ExpiryUtc
        FROM [PortalDB].[dbo].[ClientCredentials]
        WHERE CompanyId = @CompanyId
          AND IsActive = 1
          AND (ExpiryUtc IS NULL OR ExpiryUtc > SYSUTCDATETIME())
        ORDER BY CreatedUtc DESC
        """;

    public async Task<ClientCredential?> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ClientCredential>(new CommandDefinition(
            Sql, new { CompanyId = companyId }, cancellationToken: cancellationToken));
    }
}
