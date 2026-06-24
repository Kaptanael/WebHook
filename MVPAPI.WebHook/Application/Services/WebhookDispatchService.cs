using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookDispatchService(
    IWebhookEventRepository eventRepository,
    IWebhookEndpointRepository endpointRepository,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEventLifecycleService eventLifecycle,
    IWebhookDeliveryClient deliveryClient,
    ITokenDecoder tokenDecoder,
    IAccountApiClient accountApiClient,
    IOptions<WebhookDispatchOptions> options,
    ILogger<WebhookDispatchService> logger) : IWebhookDispatchService
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

        // Deliver up to MaxDeliveryConcurrency events at once so one slow endpoint can't stall the whole
        // batch. Each delivery uses its own DB connection (Dapper opens per call) and the shared HttpClient
        // is thread-safe, so concurrent delivery is safe. Outcomes are counted lock-free.
        var delivered = 0;
        var failed = 0;
        var maxConcurrency = Math.Max(1, options.Value.MaxDeliveryConcurrency);

        await Parallel.ForEachAsync(
            claimedEvents,
            new ParallelOptions { MaxDegreeOfParallelism = maxConcurrency, CancellationToken = cancellationToken },
            async (webhookEvent, ct) =>
            {
                var result = await DeliverAsync(webhookEvent, ct);

                if (result.Success)
                {
                    await eventLifecycle.MarkCompletedAsync(webhookEvent.Id, ct);
                    Interlocked.Increment(ref delivered);
                }
                else
                {
                    await eventLifecycle.MarkFailedAsync(webhookEvent.Id, result.Error ?? "Delivery failed.", ct);
                    Interlocked.Increment(ref failed);
                }
            });

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
            await eventLifecycle.MarkFailedAsync(
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
        {
            logger.LogWarning("Event {EventId}: endpoint {EndpointId} no longer exists; skipping delivery.", webhookEvent.Id, webhookEvent.WebhookId);
            return DeliveryResult.Fail("Endpoint no longer exists for the event.");
        }

        var connection = await connectionRepository.GetByClientTokenAsync(endpoint.EndPointToken, cancellationToken);
        if (connection is null)
        {
            logger.LogWarning("Event {EventId}: connection for endpoint {EndpointId} no longer exists; skipping delivery.", webhookEvent.Id, endpoint.Id);
            return DeliveryResult.Fail("Connection no longer exists for the endpoint.");
        }

        logger.LogInformation("Delivering event {EventId} (type {EventType}) to {Endpoint}.", webhookEvent.Id, webhookEvent.EventType, endpoint.Endpoint);

        var delivery = new WebhookDelivery(
            webhookEvent.Id,
            endpoint.Endpoint,
            webhookEvent.EventType,
            webhookEvent.Payload,
            endpoint.EndPointToken,
            connection.MVPApiToken,
            connection.MVPApiRefreshToken);

        var result = await deliveryClient.DeliverAsync(delivery, cancellationToken);

        if (!result.IsUnauthorized)
        {
            if (!result.Success)
                logger.LogWarning("Delivery failed for event {EventId}: {Error}", webhookEvent.Id, result.Error);
            return result;
        }

        // Bearer token expired — refresh and retry once.
        logger.LogInformation("Received 401 for event {EventId}; attempting token refresh.", webhookEvent.Id);

        var decodeResult = tokenDecoder.Decode(endpoint.EndPointToken);
        if (!decodeResult.IsSuccess)
        {
            logger.LogWarning("Token refresh aborted for event {EventId} — could not decode endpoint token: {Error}", webhookEvent.Id, decodeResult.Error);
            return DeliveryResult.Fail($"Token refresh aborted — could not decode endpoint token: {decodeResult.Error}");
        }

        var decoded = decodeResult.Value!;
        var refreshResult = await accountApiClient.GetRefreshTokenAsync(
            apiKey: decoded.ApiKey,
            clientId: decoded.ClientId,
            clientSecret: decoded.ClientSecret,
            refreshToken: connection.MVPApiRefreshToken,
            ct: cancellationToken);

        if (!refreshResult.IsSuccess)
        {
            logger.LogWarning("Token refresh failed for event {EventId}: {Error}", webhookEvent.Id, refreshResult.Error);
            return DeliveryResult.Fail($"Token refresh failed: {refreshResult.Error}");
        }

        var newToken = refreshResult.Value!;
        connection.MVPApiToken = newToken.Token;
        connection.MVPApiRefreshToken = newToken.RefreshToken;
        connection.MVPApiExpiresIn = newToken.ExpiresIn;
        await connectionRepository.UpdateAsync(connection, cancellationToken);

        logger.LogInformation("Token refreshed for event {EventId}; retrying delivery.", webhookEvent.Id);
        return await deliveryClient.DeliverAsync(delivery with { MVPApiToken = newToken.Token }, cancellationToken);
    }
}
