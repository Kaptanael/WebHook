namespace MVPAPI.WebHook.Application.Interfaces.Services;

public record DispatchSummary(int Claimed, int Delivered, int Failed);

public interface IWebhookDispatchService
{
    Task<DispatchSummary> DispatchDueEventsAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<int> RecoverStaleClaimsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
