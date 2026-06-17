using Microsoft.Data.SqlClient;

namespace MVPAPI.WebHook.Infrastructure.Persistence;

public class SqlConnectionFactory(string connectionString) : IWebhookDbConnectionFactory, IMvpEventDbConnectionFactory
{
    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
