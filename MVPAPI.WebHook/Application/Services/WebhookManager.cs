using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Options;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookManager(IAccountApiClient accountApiClient,
    IWebHookConnectionRepository connectionRepository,
    IWebhookOutboundRepository endpointRepository,
    IOptions<WebhookRouteOptions> routeOptions,
    ILogger<WebhookManager> logger) : IWebhookManager
{
    public async Task<Result<WebHookConnection>> EnsureConnectionAsync(
    string token,
    CancellationToken cancellationToken = default)
    {
        var decodeResult = ClientTokenConverter.Decode(token);
        if (!decodeResult.IsSuccess)
        {
            logger.LogWarning("Connection refused — token decode failed: {Error}", decodeResult.Error);
            return Result.Failure<WebHookConnection>(decodeResult.Error!);
        }

        var decoded = decodeResult.Value!;

        // Check for existing connection BEFORE making the external API call
        var existing = await connectionRepository.GetByClientTokenAsync(token, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Existing connection found for company {CompanyId}.", decoded.CompanyId);
            return Result.Success(existing);
        }

        var tokenResult = await accountApiClient.GetTokenAsync(
            apiKey: decoded.ApiKey,
            grantType: "client_credentials",
            clientId: decoded.ClientId,
            clientSecret: decoded.ClientSecret,
            companyId: decoded.CompanyId,
            cancellationToken);

        if (!tokenResult.IsSuccess)
        {
            logger.LogWarning(
                "Connection refused for company {CompanyId} — MVP API token request failed: {Error}",
                decoded.CompanyId, tokenResult.Error);
            return Result.Failure<WebHookConnection>(tokenResult.Error!);
        }

        var tokenResponse = tokenResult.Value!;

        var authKeyJson = JsonSerializer.Serialize(new
        {
            decoded.ApplicationName,
            decoded.CompanyId,
            decoded.ApiKey,
            decoded.ClientId,
            decoded.ClientSecret,
        });

        var connection = new WebHookConnection
        {
            CompanyId = decoded.CompanyId,
            ApplicationName = decoded.ApplicationName,
            IsActive = true,
            ClientToken = token,
            MVPApiToken = tokenResponse.Token,
            MVPApiRefreshToken = tokenResponse.RefreshToken,
            MVPApiExpiresIn = tokenResponse.ExpiresIn,
            MVPAuthKeyJson = authKeyJson,
        };

        await connectionRepository.AddAsync(connection, cancellationToken);
        logger.LogInformation(
            "New connection created for company {CompanyId} with application {ApplicationName}.",
            decoded.CompanyId, decoded.ApplicationName);

        return Result.Success(connection);
    }

    public async Task<Result<WebhookOutbound>> EnsureRegisteredAsync(
    string token, 
    string eventType, 
    int companyId, 
    CancellationToken cancellationToken = default)
    {
        if (!routeOptions.Value.Routes.TryGetValue(eventType, out var internalUrl))
            return Result<WebhookOutbound>.Failure("Route not found.");

        var existing = await endpointRepository.GetActiveByTokenAsync(token, cancellationToken);
        if (existing is not null)
            return Result<WebhookOutbound>.Success(existing);

        var endpoint = new WebhookOutbound
        {
            EndPointToken = token,
            Endpoint = internalUrl,
            CompanyId = companyId,
            TriggerConfigJson = JsonSerializer.Serialize(new { triggerType = eventType, companyId }),
            IsActive = true,
            ActionDataSchema = "{}"
        };

        logger.LogInformation("Auto-registering internal endpoint for company {CompanyId} with event type {EventType} -> {InternalUrl}.", companyId, eventType, internalUrl);
        await endpointRepository.AddAsync(endpoint, cancellationToken);

        return Result<WebhookOutbound>.Success(endpoint);
    }
}
