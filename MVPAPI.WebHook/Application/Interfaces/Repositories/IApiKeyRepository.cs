using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

/// <summary>Reads API keys from the external PortalDB.</summary>
public interface IApiKeyRepository
{
    /// <summary>Returns the key whose <c>RawApiKey</c> equals the presented value, or null if none.</summary>
    Task<ApiKey?> GetByRawApiKeyAsync(string rawApiKey, CancellationToken cancellationToken = default);
}
