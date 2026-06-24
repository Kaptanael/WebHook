using Microsoft.Data.SqlClient;

namespace MVPAPI.WebHook.Infrastructure.Persistence;

/// <summary>Opens a <see cref="SqlConnection"/> for a fixed connection string.</summary>
public abstract class SqlConnectionFactoryBase(string connectionString)
{
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}

public class SqlConnectionFactory(string connectionString)
    : SqlConnectionFactoryBase(connectionString), IWebhookDbConnectionFactory;

public class PortalDbConnectionFactory(string connectionString)
    : SqlConnectionFactoryBase(connectionString), IPortalDbConnectionFactory;
