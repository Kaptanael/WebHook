namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebhookEventLifecycleService
{
    Task<bool> MarkProcessingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkCompletedAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> MarkFailedAsync(Guid id, string error, CancellationToken cancellationToken = default);

    /// <summary>Requeues a permanently-Failed event for redelivery (dead-letter replay). Returns false
    /// when no Failed event with that id exists.</summary>
    Task<bool> RequeueAsync(Guid id, CancellationToken cancellationToken = default);
}
