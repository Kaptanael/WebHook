using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;

namespace MVPAPI.WebHook.BackgroundServices;

public class OutboxProcessorService(
    IServiceScopeFactory scopeFactory,
    IOptions<WebhookDispatchOptions> options,
    ILogger<OutboxProcessorService> logger) : BackgroundService
{
    private const int MaxAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);
        logger.LogInformation("Outbox processor started. Polling every {Interval}s, batch size {BatchSize}.",
            options.Value.PollingIntervalSeconds, options.Value.BatchSize);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing cycle failed.");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));

        logger.LogInformation("Outbox processor stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outboxRepo   = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var endpointRepo = scope.ServiceProvider.GetRequiredService<IWebhookEndpointRepository>();
        var eventRepo    = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();

        var messages = await outboxRepo.ClaimPendingAsync(options.Value.BatchSize, MaxAttempts, cancellationToken);
        if (messages.Count == 0) return;

        foreach (var message in messages)
        {
            try
            {
                var endpoints = await endpointRepo.GetActiveByEventTypeAsync(message.EventType, cancellationToken);

                foreach (var endpoint in endpoints)
                {
                    await eventRepo.AddAsync(new WebhookEvent
                    {
                        WebhookId        = endpoint.Id,
                        Provider         = message.Provider,
                        EventType        = message.EventType,
                        Payload          = message.Payload,
                        Status           = EventStatus.Pending,
                        ReceivedAtUtc    = message.CreatedAtUtc,
                        NextAttemptAtUtc = DateTime.UtcNow
                    }, cancellationToken);
                }

                await outboxRepo.MarkProcessedAsync(message.Id, DateTime.UtcNow, cancellationToken);
                logger.LogInformation(
                    "Outbox {MessageId} processed: {Count} event(s) queued for type '{EventType}'.",
                    message.Id, endpoints.Count, message.EventType);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox message {MessageId}.", message.Id);
                await outboxRepo.MarkFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try { return await timer.WaitForNextTickAsync(stoppingToken); }
        catch (OperationCanceledException) { return false; }
    }
}
