using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Outbounds;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookEndpointService
{
    Task<Result<SubscribeResponse>> SubscribeAsync(string token, SubscribeRequest request, CancellationToken cancellationToken = default);
    Task<Result<UnsubscribeResponse>> UnsubscribeAsync(string token, Guid subscriberId, CancellationToken cancellationToken = default);
    Task<Result<WebhookSchemaResponse>> GetSchemaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookOutboundResponse>> GetByClientTokenAsync(string clientToken, CancellationToken cancellationToken = default);
}
