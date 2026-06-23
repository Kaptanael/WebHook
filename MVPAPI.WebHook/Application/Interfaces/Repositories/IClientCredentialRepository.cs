using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

/// <summary>Reads OAuth client credentials from the external PortalDB.</summary>
public interface IClientCredentialRepository
{
    /// <summary>Returns the active, unexpired credential for the company, or null if none.</summary>
    Task<ClientCredential?> GetActiveByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
}
