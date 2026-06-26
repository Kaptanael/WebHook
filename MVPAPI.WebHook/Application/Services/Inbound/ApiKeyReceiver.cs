using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services.Inbound;

// Receiver for the x-api-key-only scheme (no signature): the key is accepted when it matches an active
// PortalDB row. Weaker than the full Standard Webhooks scheme. Normalizes and queues the inbound event.
public class ApiKeyReceiver(
    IApiKeyRepository apiKeyRepository,
    PayloadNormalizer normalizer,
    IWebhookInboundService eventService,
    ILogger<ApiKeyReceiver> logger)
{
    public async Task<InboundResult> HandleAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Read the api-key from the x-api-key header.
        if (!request.Headers.TryGetValue(WebhookHeaders.ApiKeyHeader, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Inbound rejected — missing '{Header}' header.", WebhookHeaders.ApiKeyHeader);
            return InboundResult.Unauthorized("API key is missing.");
        }

        // 2. Accept the key when it matches an active PortalDB row.
        var key = await apiKeyRepository.GetByRawApiKeyAsync(apiKey, cancellationToken);
        if (key is null || !key.IsActive)
        {
            logger.LogWarning("Inbound rejected — no active api-key matched.");
            return InboundResult.Unauthorized("Invalid API key.");
        }

        // 3. Normalize the payload.
        var normalizeResult = normalizer.Normalize(request);
        if (!normalizeResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — normalization failed for company {CompanyId}: {Error}", key.CompanyId, normalizeResult.Error);
            return InboundResult.Invalid(normalizeResult.Error!);
        }

        // 4. Queue the event.
        var normalized = normalizeResult.Value!;
        //await eventService.PublishToEndpointAsync((int)key.CompanyId, normalized.EventType, normalized.Payload, key.ApplicationName, cancellationToken);

        logger.LogInformation("Inbound accepted for company {CompanyId} — queued event of type {EventType}.", key.CompanyId, normalized.EventType);
        return InboundResult.Accepted(1);
    }
}
