using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services.Inbound;

// Receiver for the full Standard Webhooks scheme: an x-api-key plus the signed
// webhook-id/webhook-timestamp/webhook-signature triplet. Verifies the signature, rebuilds the tenant's
// client token from its PortalDB credential, ensures the MVP API connection, then normalizes and queues
// the inbound event.
public class StandardWebhookReceiver(
    [FromKeyedServices("standard")] IInboundAuthenticator inboundWebhookAuth,
    PayloadNormalizer normalizer,
    IWebhookInboundService eventService,    
    IClientCredentialRepository clientCredentialRepository,
    IWebhookManager webhookManager,
    ILogger<StandardWebhookReceiver> logger)
{
    public async Task<InboundResult> HandleAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Authenticate the api-key + signed triplet.
        var authenticationResult = await inboundWebhookAuth.AuthenticateAsync(request, cancellationToken);
        if (!authenticationResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — authentication error: {Error}", authenticationResult.Error);
            return InboundResult.Unauthorized("Authentication failed.");
        }

        var authentication = authenticationResult.Value!;               

        // 2. Rebuild the tenant's client token from its PortalDB credential + key.
        var clientCredential = await clientCredentialRepository.GetActiveByCompanyIdAsync(authentication.CompanyId);
        if (clientCredential is null)
        {
            logger.LogWarning("Inbound rejected — no active client credential for company {CompanyId}.", authentication.CompanyId);
            return InboundResult.Unauthorized("Client credential is invalid.");
        }        

        var clientTokenResult = ClientTokenConverter.Encode(
            "http://localhost", 
            authentication.apiKey!,
            clientCredential.ClientId,
            clientCredential.Secret,
            authentication.ApplicationName!,
            authentication.CompanyId.ToString());

        if (!clientTokenResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — could not build client token for company {CompanyId}: {Error}", authentication.CompanyId, clientTokenResult.Error);
            return InboundResult.Invalid(clientTokenResult.Error!);
        }

        var clientToken = clientTokenResult.Value!;

        // 3. Ensure the tenant connection is active.
        var connectionResult = await webhookManager.EnsureConnectionAsync(clientToken, cancellationToken);
        if (!connectionResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — no active connection for company {CompanyId}: {Error}", authentication.CompanyId, connectionResult.Error);
            return InboundResult.Unauthorized($"No active connection: {connectionResult.Error}");
        }

        // 4. Normalize the payload.
        var normalizeResult = normalizer.Normalize(request);
        if (!normalizeResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — normalization failed for company {CompanyId}: {Error}", authentication.CompanyId, normalizeResult.Error);
            return InboundResult.Invalid(normalizeResult.Error!);
        }

        // 5. Queue the event.
        var normalized = normalizeResult.Value!;
        await eventService.PublishToEndpointAsync(connectionResult.Value!.Id, authentication.CompanyId, normalized.EventType, normalized.Payload, authentication.ApplicationName, cancellationToken);

        logger.LogInformation("Inbound accepted for company {CompanyId} — queued event of type {EventType}.", authentication.CompanyId, normalized.EventType);
        return InboundResult.Accepted(1);
    }
}
