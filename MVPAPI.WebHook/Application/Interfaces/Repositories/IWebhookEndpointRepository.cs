using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IWebhookEndpointRepository
{
    Task<WebhookEndpoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WebhookEndpoint?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<WebhookEndpoint?> GetByEndpointTokenAsync(string endpointToken, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookEndpoint>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookEndpoint>> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookEndpoint>> GetActiveByCompanyAndEventTypeAsync(int companyId, string eventType, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
