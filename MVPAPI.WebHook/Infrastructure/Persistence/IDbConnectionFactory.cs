using Microsoft.Data.SqlClient;

namespace MVPAPI.WebHook.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

// Resolves to MVPWebhookDB
public interface IWebhookDbConnectionFactory : IDbConnectionFactory { }

// Resolves to MVPEventDB (caLiveEvents_MVP_02152024)
public interface IMvpEventDbConnectionFactory : IDbConnectionFactory { }
