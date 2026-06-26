using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IApiKeyRepository
{    
    Task<ApiKey?> GetByRawApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default);
}
