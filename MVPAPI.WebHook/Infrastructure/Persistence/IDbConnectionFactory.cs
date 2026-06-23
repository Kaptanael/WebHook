using Microsoft.Data.SqlClient;

namespace MVPAPI.WebHook.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public interface IWebhookDbConnectionFactory : IDbConnectionFactory { }

/// <summary>Connects to the external PortalDB, source of the <c>ApiKeys</c> rows whose
/// <c>Salt</c> is the shared secret for inbound Standard Webhooks signature verification.</summary>
public interface IPortalDbConnectionFactory : IDbConnectionFactory { }
