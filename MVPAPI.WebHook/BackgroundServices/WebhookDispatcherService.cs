using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.BackgroundServices;

public class WebhookDispatcherService(
    IServiceScopeFactory scopeFactory,
    IOptions<WebhookDispatchOptions> options,
    ILogger<WebhookDispatcherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);
        var staleClaimTimeout = TimeSpan.FromSeconds(options.Value.StaleClaimTimeoutSeconds);
        logger.LogInformation(
            "Webhook dispatcher started. Polling every {Interval}s, batch size {BatchSize}, stale-claim timeout {StaleTimeout}s.",
            options.Value.PollingIntervalSeconds, options.Value.BatchSize, options.Value.StaleClaimTimeoutSeconds);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<IWebhookDispatchService>();

                var recovered = await dispatchService.RecoverStaleClaimsAsync(staleClaimTimeout, stoppingToken);
                if (recovered > 0)
                {
                    logger.LogWarning("Recovered {Recovered} event(s) stuck in Processing state.", recovered);
                }

                var summary = await dispatchService.DispatchDueEventsAsync(options.Value.BatchSize, stoppingToken);

                if (summary.Claimed > 0)
                {
                    logger.LogInformation(
                        "Dispatched {Claimed} event(s): {Delivered} delivered, {Failed} failed.",
                        summary.Claimed, summary.Delivered, summary.Failed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Webhook dispatch cycle failed.");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));

        logger.LogInformation("Webhook dispatcher stopped.");
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
