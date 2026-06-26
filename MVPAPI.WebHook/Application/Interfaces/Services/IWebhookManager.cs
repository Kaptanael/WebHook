using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Services
{
    public interface IWebhookManager
    {
        Task<Result<WebHookConnection>> EnsureConnectionAsync(
        string token,
        CancellationToken cancellationToken = default);

        Task<Result<WebhookOutbound>> EnsureRegisteredAsync(
        string token,
        string eventType,
        int companyId,
        CancellationToken cancellationToken = default);
    }
}
