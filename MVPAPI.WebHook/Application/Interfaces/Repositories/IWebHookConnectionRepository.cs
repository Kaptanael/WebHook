using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IWebHookConnectionRepository
{
    Task<WebHookConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WebHookConnection?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<WebHookConnection?> GetByClientTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);    
    Task<Guid> AddAsync(WebHookConnection connection, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(WebHookConnection connection, CancellationToken cancellationToken = default);
}
