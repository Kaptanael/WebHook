using Microsoft.Extensions.Logging;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class InboundWebhookPipeline(
    IWebhookEndpointRepository endpointRepository,
    IWebHookConnectionRepository connectionRepository,
    IApiKeyInboundResolver apiKeyResolver,
    IInboundProvisioningService provisioningService,
    IEnumerable<IInboundAuthenticator> authenticators,
    IPayloadAdapter adapter,
    IWebhookEventService eventService,
    ITokenDecoder tokenDecoder,
    ILogger<InboundWebhookPipeline> logger) : IInboundWebhookPipeline
{
    public async Task<InboundResult> ProcessAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Resolve the sending endpoint. There is no key in the route, so the presented credential
        //    identifies the sender. An authenticated key with no endpoint yet is auto-provisioned.
        var (endpoint, failure) = await ResolveEndpointAsync(request, cancellationToken);
        if (failure is not null)
        {
            logger.LogWarning("Inbound rejected — {Error}", failure.Error);
            return failure;
        }
        if (endpoint is null)
        {
            logger.LogWarning("Inbound rejected — no active endpoint matched the request credentials.");
            return InboundResult.Unauthorized("No active endpoint matched the request credentials.");
        }

        // 2. Resolve the tenant connection via the endpoint's token. A company can own several
        //    connections, but ClientToken is unique, and an endpoint's EndPointToken IS its
        //    connection's ClientToken (the same link the dispatcher uses to deliver). Resolving by
        //    CompanyId would be ambiguous and throws when more than one connection exists.
        var connection = await connectionRepository.GetByClientTokenAsync(endpoint.EndPointToken, cancellationToken);
        if (connection is null || !connection.IsActive)
        {
            logger.LogError("Endpoint '{Endpoint}' (company {CompanyId}) has no active connection.", endpoint.Endpoint, endpoint.CompanyId);
            return InboundResult.Invalid($"No active connection for endpoint '{endpoint.Endpoint}'.");
        }

        // 3. Normalize the payload to an internal event.
        var normalizeResult = adapter.Normalize(request, endpoint);
        if (!normalizeResult.IsSuccess)
        {
            logger.LogWarning("Inbound rejected — normalization failed for endpoint '{Endpoint}': {Error}", endpoint.Endpoint, normalizeResult.Error);
            return InboundResult.Invalid(normalizeResult.Error!);
        }

        // 4. Queue a single event for the resolved endpoint only (no company-wide fan-out): the inbound
        //    credential is bound to exactly one endpoint, so the event belongs to that endpoint.
        var normalized = normalizeResult.Value!;
        await eventService.PublishToEndpointAsync(endpoint, normalized.EventType, normalized.Payload, normalized.Provider, cancellationToken);

        logger.LogInformation("Inbound accepted for endpoint '{Endpoint}' (company {CompanyId}) — queued event of type {EventType}.",
            endpoint.Endpoint, endpoint.CompanyId, normalized.EventType);
        return InboundResult.Accepted(1);
    }

    /// <summary>
    /// Identifies the sending endpoint. Tries the full Standard Webhooks path first: an <c>X-Api-Key</c>
    /// selects a PortalDB key and the signed triplet is verified against its salt; the endpoint is then
    /// the company's active endpoint. If no <c>X-Api-Key</c> is presented, falls back to per-endpoint
    /// credential matching (custom token / signing secret).
    /// </summary>
    /// <returns>
    /// The matched endpoint, or a populated <see cref="InboundResult"/> failure: <c>Unauthorized</c> when a
    /// presented credential is invalid, <c>Invalid</c> when the key is valid but no endpoint could be
    /// resolved or provisioned. (null, null) means no credential matched (caller treats as unauthorized).
    /// </returns>
    private async Task<(WebhookEndpoint? Endpoint, InboundResult? Failure)> ResolveEndpointAsync(
        InboundRequest request, CancellationToken cancellationToken)
    {
        var apiKeyAuth = await apiKeyResolver.ResolveAsync(request, cancellationToken);
        if (apiKeyAuth.Outcome == ApiKeyAuthOutcome.Rejected)
            return (null, InboundResult.Unauthorized(apiKeyAuth.Error!));

        if (apiKeyAuth.Outcome == ApiKeyAuthOutcome.Authenticated)
        {
            // Bind to the one endpoint this key owns: its EndPointToken (a ConfigEncoder blob) decodes
            // back to the same RawApiKey. This is precise even when the company has several endpoints.
            var companyEndpoints = await endpointRepository.GetByCompanyIdAsync(apiKeyAuth.CompanyId, cancellationToken);
            var endpoint = companyEndpoints.FirstOrDefault(e => e.IsActive && IsBoundToApiKey(e, apiKeyAuth.RawApiKey!));
            if (endpoint is not null)
                return (endpoint, null);

            // Valid key but nothing provisioned yet — auto-provision from the company's client credentials.
            // The delivery URL is keyed by event type, so the event type header is required here.
            if (!request.Headers.TryGetValue(DefaultPayloadAdapter.EventTypeHeader, out var eventType) || string.IsNullOrWhiteSpace(eventType))
                return (null, InboundResult.Invalid($"Missing {DefaultPayloadAdapter.EventTypeHeader} header."));

            var provisioned = await provisioningService.EnsureProvisionedAsync(
                apiKeyAuth.CompanyId, apiKeyAuth.RawApiKey!, eventType.Trim(), cancellationToken);
            if (provisioned is null)
                return (null, InboundResult.Invalid(
                    $"API key is valid but no webhook endpoint is provisioned for company {apiKeyAuth.CompanyId}, and it could not be auto-provisioned (no active client credential or no route for event type '{eventType.Trim()}')."));
            return (provisioned, null);
        }

        // No X-Api-Key: per-endpoint credential matching.
        var endpoints = await endpointRepository.GetActiveAsync(cancellationToken);
        foreach (var endpoint in endpoints)
        {
            foreach (var authenticator in authenticators)
            {
                if (authenticator.Authenticate(request, endpoint).IsSuccess)
                    return (endpoint, null);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// True when the endpoint belongs to the given API key: its <see cref="WebhookEndpoint.EndPointToken"/>
    /// decodes to a token whose embedded ApiKey equals <paramref name="rawApiKey"/>.
    /// </summary>
    private bool IsBoundToApiKey(WebhookEndpoint endpoint, string rawApiKey)
    {
        var decoded = tokenDecoder.Decode(endpoint.EndPointToken);
        return decoded.IsSuccess && string.Equals(decoded.Value!.ApiKey, rawApiKey, StringComparison.Ordinal);
    }
}
