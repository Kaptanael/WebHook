using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Options;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class InboundProvisioningService(
    IClientCredentialRepository credentialRepository,
    IWebhookManager webhookManager,
    IWebhookOutboundRepository endpointRepository,
    IOptions<WebhookRouteOptions> routeOptions,
    IOptions<MVPApiOptions> apiOptions,
    ILogger<InboundProvisioningService> logger) : IInboundProvisioningService
{
    // All inbound-provisioned connections are tagged with this application type in their client token.
    private const string ApplicationType = "API_MVP_INTEGRATION_ZAPIER";

    public async Task<WebhookOutbound?> EnsureProvisionedAsync(int companyId, string rawApiKey, string eventType, CancellationToken cancellationToken = default)
    {
        // The endpoint's delivery URL comes from the WebhookRoutes config, keyed by event type.
        if (!routeOptions.Value.Routes.TryGetValue(eventType, out var deliveryUrl) || string.IsNullOrWhiteSpace(deliveryUrl))
        {
            logger.LogWarning("Cannot provision endpoint for company {CompanyId}: no WebhookRoutes entry for event type '{EventType}'.", companyId, eventType);
            return null;
        }

        // clientId/secret are not in the inbound request; the only server-side source is ClientCredentials.
        var credential = await credentialRepository.GetActiveByCompanyIdAsync(companyId, cancellationToken);
        if (credential is null)
        {
            logger.LogWarning("Cannot provision connection for company {CompanyId}: no active client credential.", companyId);
            return null;
        }

        var tokenResult = ClientTokenConverter.Encode(
            apiOptions.Value.BaseUrl, rawApiKey, credential.ClientId, credential.Secret, ApplicationType, companyId.ToString());
        if (!tokenResult.IsSuccess)
        {
            logger.LogError("Cannot provision connection for company {CompanyId}: {Error}", companyId, tokenResult.Error);
            return null;
        }

        var clientToken = tokenResult.Value!;

        // Obtain the MVP API token and persist the connection (idempotent on ClientToken).
        var connectionResult = await webhookManager.EnsureConnectionAsync(clientToken, cancellationToken);
        if (!connectionResult.IsSuccess)
        {
            logger.LogError("Inbound provisioning failed for company {CompanyId}: {Error}", companyId, connectionResult.Error);
            return null;
        }

        // Reuse an existing endpoint for this event type if present; otherwise create it.
        var existing = await endpointRepository.GetActiveByCompanyAndEventTypeAsync(companyId, eventType, cancellationToken);
        if (existing.Count > 0)
            return existing[0];

        var endpoint = new WebhookOutbound
        {
            EndPointToken = clientToken,
            Endpoint = deliveryUrl,
            CompanyId = companyId,
            TriggerConfigJson = JsonSerializer.Serialize(new { triggerType = eventType, companyId }),
            IsActive = true,
            ActionDataSchema = "{}"
        };
        endpoint.Id = await endpointRepository.AddAsync(endpoint, cancellationToken);
        logger.LogInformation("Provisioned inbound endpoint {EndpointId} for company {CompanyId}, event type {EventType} -> {Url}.",
            endpoint.Id, companyId, eventType, deliveryUrl);
        return endpoint;
    }
}
