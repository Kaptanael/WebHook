using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookDispatchService(
    IWebhookEventRepository eventRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEventService eventService,
    IWebhookDeliveryClient deliveryClient) : IWebhookDispatchService
{
    public async Task<DispatchSummary> DispatchDueEventsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        // Claiming happens atomically in the database (Pending/Retrying -> Processing),
        // so concurrent instances never pick up the same events.
        var claimedEvents = await eventRepository.ClaimDueForProcessingAsync(batchSize, DateTime.UtcNow, cancellationToken);
        if (claimedEvents.Count == 0)
        {
            return new DispatchSummary(0, 0, 0);
        }

        var delivered = 0;
        var failed = 0;

        foreach (var webhookEvent in claimedEvents)
        {
            var result = await DeliverAsync(webhookEvent, cancellationToken);

            if (result.Success)
            {
                await eventService.MarkCompletedAsync(webhookEvent.Id, cancellationToken);
                delivered++;
            }
            else
            {
                await eventService.MarkFailedAsync(webhookEvent.Id, result.Error ?? "Delivery failed.", cancellationToken);
                failed++;
            }
        }

        return new DispatchSummary(claimedEvents.Count, delivered, failed);
    }

    public async Task<int> RecoverStaleClaimsAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var staleEvents = await eventRepository.ClaimStaleProcessingAsync(nowUtc - olderThan, nowUtc, cancellationToken);

        foreach (var staleEvent in staleEvents)
        {
            // Counts as a failed attempt so a poison event cannot crash-loop forever:
            // it re-enters the queue with backoff and hits the Failed cap eventually.
            await eventService.MarkFailedAsync(
                staleEvent.Id,
                $"Recovered from stale Processing state (exceeded {olderThan.TotalSeconds:F0}s claim timeout).",
                cancellationToken);
        }

        return staleEvents.Count;
    }

    private async Task<DeliveryResult> DeliverAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        var endpoint = await endpointRepository.GetByIdAsync(webhookEvent.WebhookId, cancellationToken);
        if (endpoint is null)
            return DeliveryResult.Fail("Endpoint no longer exists for the event.");

        var connection = await connectionRepository.GetByClientTokenAsync(endpoint.EndPointToken, cancellationToken);
        if (connection is null)
            return DeliveryResult.Fail("Connection no longer exists for the endpoint.");

        var delivery = new WebhookDelivery(
            webhookEvent.Id,
            endpoint.Endpoint,
            webhookEvent.EventType,
            webhookEvent.Payload,
            endpoint.EndPointToken,
            connection.MVPApiToken,
            connection.MVPApiRefreshToken);

        return await deliveryClient.DeliverAsync(delivery, cancellationToken);
    }
}
