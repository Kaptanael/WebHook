namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookEventLifecycleService
{
    Task<bool> MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);
}
