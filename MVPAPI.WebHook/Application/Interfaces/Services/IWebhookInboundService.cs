using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Events;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookInboundService
{
    Task<Result<IReadOnlyList<WebhookInboundResponse>>> PublishEventAsync(EventRequest request, string token, string signature, string timestamp, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookInboundResponse>> PublishAsync(PublishEventRequest request, CancellationToken cancellationToken = default);
    /// <summary>Fans an inbound event out to the company's active endpoints subscribed to the event type.</summary>
    Task<IReadOnlyList<WebhookInboundResponse>> PublishToEndpointAsync(Guid webhookId, int companyId, string eventType, string payload, string? provider, CancellationToken cancellationToken = default);
    Task<WebhookInboundResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebhookInboundResponse>> GetDueForProcessingAsync(int batchSize, CancellationToken cancellationToken = default);
    /// <summary>The most recent permanently-Failed events (dead-letter view).</summary>
    Task<IReadOnlyList<WebhookInboundResponse>> GetFailedAsync(int limit, CancellationToken cancellationToken = default);
}
