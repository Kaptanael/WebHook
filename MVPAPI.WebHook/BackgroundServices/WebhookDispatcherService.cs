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
        var badgeSize = options.Value.BatchSize;

        // The background service responsible for processing queued webhook events has started running.
        // Every 10 seconds the dispatcher checks the database/queue for pending webhook events.
        // On each poll, it processes up to 100 events at a time.
        // If a worker claims an event for processing but does not finish within 300 seconds (5 minutes),
        // the event is considered "stale" and can be picked up again for retry.

        logger.LogInformation("Webhook dispatcher started.");
        logger.LogInformation($"Polling every {interval}s, batch size {badgeSize}, stale-claim timeout {staleClaimTimeout}s.");            

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var dispatchService = scope.ServiceProvider.GetRequiredService<IWebhookDispatchService>();

                // First, recover any events that are stuck in the Processing state beyond the stale claim timeout.
                var recovered = await dispatchService.RecoverStaleClaimsAsync(staleClaimTimeout, stoppingToken);
                if (recovered > 0)
                {
                    logger.LogWarning("Recovered {Recovered} event(s) stuck in Processing state.", recovered);
                }

                // Then, claim and dispatch due events up to the batch size limit.
                var summary = await dispatchService.DispatchDueEventsAsync(options.Value.BatchSize, stoppingToken);
                if (summary.Claimed > 0)
                {
                    logger.LogInformation($"Dispatched {summary.Claimed} event(s): {summary.Delivered} delivered, {summary.Failed} failed.");                        
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
