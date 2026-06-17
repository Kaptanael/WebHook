using Microsoft.Data.SqlClient;

namespace MVPAPI.WebHook.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public interface IWebhookDbConnectionFactory : IDbConnectionFactory { }

public interface IMvpEventDbConnectionFactory : IDbConnectionFactory { }
