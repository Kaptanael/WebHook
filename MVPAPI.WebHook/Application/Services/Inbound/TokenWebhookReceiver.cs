using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Helpers;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class TokenWebhookReceiver(
    PayloadNormalizer normalizer,
    IWebhookManager webhookManager,
    IWebhookInboundService eventService,
    [FromKeyedServices("token")] IInboundAuthenticator inboundWebhookAuth,
    ILogger<TokenWebhookReceiver> logger)
{
    public async Task<InboundResult> HandleAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Read the client token from the x-token header.
        request.Headers.TryGetValue(WebhookHeaders.TokenHeader, out var clientToekn);

        // 2. Authenticate the request using the inbound authenticator.
        var authenticationResult = await inboundWebhookAuth.AuthenticateAsync(request, cancellationToken);
        if (!authenticationResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — authentication error: {Error}", authenticationResult.Error);
            return InboundResult.Unauthorized("Authentication failed.");
        }

        var authentication = authenticationResult.Value!;
        if (authentication.Outcome == InboundAuthOutcome.Rejected)
        {
            logger.LogWarning("Inbound rejected — {Error}", authentication.Error);
            return InboundResult.Unauthorized(authentication.Error!);
        }

        // 3. Read the event type from the body's "type".
        var eventType = JsonHelper.GetType(request.RawBody);
        if (eventType is null)
        {
            logger.LogWarning("Inbound rejected — missing or empty 'type' field in the request body.");
            return InboundResult.Unauthorized("Missing or empty 'type' field in JSON.");
        }

        // 4. Ensure the tenant connection is active.
        var connectionResult = await webhookManager.EnsureConnectionAsync(clientToekn!, cancellationToken);
        if (!connectionResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — no active connection: {Error}", connectionResult.Error);
            return InboundResult.Unauthorized($"No active connection: {connectionResult.Error}");
        }

        var connection = connectionResult.Value!;

        // 5. Normalize the payload.
        var normalizeResult = normalizer.Normalize(request);
        if (!normalizeResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — normalization failed for company {CompanyId}: {Error}", connection.CompanyId, normalizeResult.Error);
            return InboundResult.Invalid(normalizeResult.Error!);
        }

        // 6. Queue the event.
        var normalized = normalizeResult.Value!;
        await eventService.PublishToEndpointAsync(connection.Id, authentication.CompanyId, normalized.EventType, normalized.Payload, authentication.ApplicationName, cancellationToken);

        logger.LogInformation("Inbound accepted for company {CompanyId} — queued event of type {EventType}.", connection.CompanyId, normalized.EventType);
        return InboundResult.Accepted(1);
    }
}
