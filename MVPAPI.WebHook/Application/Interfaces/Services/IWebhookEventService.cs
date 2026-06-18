using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Events;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookEventService
{
    Task<Result<IReadOnlyList<EventResponse>>> PublishEventAsync(EventRequest request, string token, string signature, string timestamp, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventResponse>> PublishAsync(PublishEventRequest request, CancellationToken cancellationToken = default);    
    Task<EventResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventResponse>> GetDueForProcessingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<bool> MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
}
