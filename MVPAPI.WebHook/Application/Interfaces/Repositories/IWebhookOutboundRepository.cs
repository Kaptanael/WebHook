using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IWebhookOutboundRepository
{
    Task<WebhookOutbound?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WebhookOutbound?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    /// <summary>The active endpoint whose <c>EndPointToken</c> exactly equals <paramref name="token"/>, or null.</summary>
    Task<WebhookOutbound?> GetActiveByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookOutbound>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookOutbound>> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    Task<WebhookOutbound?> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookOutbound>> GetActiveByCompanyAndEventTypeAsync(int companyId, string eventType, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(WebhookOutbound endpoint, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(WebhookOutbound endpoint, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
