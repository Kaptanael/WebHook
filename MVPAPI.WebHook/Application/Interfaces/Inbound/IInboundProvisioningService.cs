using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

/// <summary>
/// Auto-provisions the connection + endpoint for an authenticated inbound API key whose company has none
/// yet. Builds the ClientToken from the company's PortalDB <c>ClientCredentials</c> (the only server-side
/// source of the clientId/secret needed to obtain an MVP API token).
/// </summary>
public interface IInboundProvisioningService
{
    /// <summary>
    /// Ensures an active endpoint exists for the company and event type, creating the connection and
    /// endpoint if necessary. Returns the endpoint, or null when it cannot be provisioned (no active
    /// client credential, no <c>WebhookRoutes</c> entry for the event type, or token acquisition failed).
    /// </summary>
    Task<WebhookEndpoint?> EnsureProvisionedAsync(int companyId, string rawApiKey, string eventType, CancellationToken cancellationToken = default);
}
