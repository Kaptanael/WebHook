using Microsoft.Extensions.Logging;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Endpoints;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEndpointService(
    ITokenDecoder tokenValidator,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository,
    ILogger<WebhookEndpointService> logger) : IWebhookEndpointService
{
    public async Task<Result<SubscribeResponse>> SubscribeAsync(string token, SubscribeRequest request, CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenValidator.Decode(token);
        if (!decodeResult.IsSuccess)
        {
            logger.LogWarning("Subscribe failed — token decode error: {Error}", decodeResult.Error);
            return Result.Failure<SubscribeResponse>(decodeResult.Error!);
        }

        var companyId = decodeResult.Value!.CompanyId;

        var existingWebhookEndpoint = await endpointRepository.GetByEndpointAsync(request.Endpoint, cancellationToken);
        if (existingWebhookEndpoint is not null)
        {
            logger.LogWarning("Subscribe failed — endpoint {Endpoint} already exists for company {CompanyId}.", request.Endpoint, companyId);
            return Result.Failure<SubscribeResponse>("Webhook endpoint already exists.");
        }

        var schemaResult = WebhookSchemaGenerator.Generate(request.TriggerConfigJson.GetRawText());
        if (!schemaResult.IsSuccess)
        {
            logger.LogWarning("Subscribe failed — schema generation error for company {CompanyId}: {Error}", companyId, schemaResult.Error);
            return Result<SubscribeResponse>.Failure(schemaResult.Error!);
        }

        var actionDataSchema = schemaResult.Value!;

        var endpoint = new WebhookEndpoint
        {
            EndPointToken = token,
            Endpoint = request.Endpoint,
            CompanyId = companyId,
            TriggerConfigJson = request.TriggerConfigJson.GetRawText(),
            ActionDataSchema = JsonSerializer.Serialize(actionDataSchema)
        };

        await endpointRepository.AddAsync(endpoint, cancellationToken);

        logger.LogInformation("Endpoint {EndpointId} subscribed for company {CompanyId} -> {Endpoint}.", endpoint.Id, companyId, request.Endpoint);
        return Result.Success(new SubscribeResponse
        {
            Id = endpoint.Id,
            RemoteEndpoint = endpoint.Endpoint
        });
    }

    public async Task<Result<UnsubscribeResponse>> UnsubscribeAsync(string token, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenValidator.Decode(token);
        if (!decodeResult.IsSuccess)
        {
            logger.LogWarning("Unsubscribe failed — token decode error: {Error}", decodeResult.Error);
            return Result.Failure<UnsubscribeResponse>(decodeResult.Error!);
        }

        var companyId = decodeResult.Value!.CompanyId;

        var endpoint = await endpointRepository.GetByIdAsync(subscriberId, cancellationToken);
        if (endpoint is null)
        {
            logger.LogWarning("Unsubscribe failed — endpoint {SubscriberId} not found.", subscriberId);
            return Result.Failure<UnsubscribeResponse>("Webhook endpoint not found.");
        }

        if (endpoint.CompanyId != companyId)
        {
            logger.LogWarning("Unsubscribe denied — endpoint {SubscriberId} belongs to company {OwnerCompanyId}, not {CompanyId}.", subscriberId, endpoint.CompanyId, companyId);
            return Result.Failure<UnsubscribeResponse>("Webhook endpoint does not belong to this company.");
        }

        var removed = await endpointRepository.DeleteAsync(endpoint.Id, cancellationToken);
        if (!removed)
        {
            logger.LogWarning("Unsubscribe failed — could not delete endpoint {SubscriberId}.", subscriberId);
            return Result.Failure<UnsubscribeResponse>("Failed to remove webhook endpoint.");
        }

        logger.LogInformation("Endpoint {SubscriberId} unsubscribed for company {CompanyId}.", subscriberId, companyId);
        return Result.Success(new UnsubscribeResponse(subscriberId));
    }

    public async Task<Result<WebhookSchemaResponse>> GetSchemaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEndpoint = await endpointRepository.GetByIdAsync(id, cancellationToken);

        if (webhookEndpoint == null)
            return Result.Failure<WebhookSchemaResponse>("Webhook endpoint not found.");

        var triggerConfig = JsonSerializer.Deserialize<TriggerConfig>(webhookEndpoint.TriggerConfigJson);
        if (triggerConfig == null)
            return Result.Failure<WebhookSchemaResponse>("Invalid trigger configuration."); 

        if (string.IsNullOrWhiteSpace(webhookEndpoint.TriggerConfigJson))
            return Result.Failure<WebhookSchemaResponse>("Trigger configuration is empty.");

        var schemaResult = WebhookSchemaGenerator.Generate(webhookEndpoint.TriggerConfigJson);
        if (!schemaResult.IsSuccess)
            return Result.Failure<WebhookSchemaResponse>(schemaResult.Error!);

        var actionDataSchema = schemaResult.Value!;

        object? storedActionDataSchema;

        try
        {
            storedActionDataSchema = string.IsNullOrWhiteSpace(webhookEndpoint.ActionDataSchema)
                ? actionDataSchema
                : JsonSerializer.Deserialize<object>(webhookEndpoint.ActionDataSchema);
        }
        catch
        {
            return Result.Failure<WebhookSchemaResponse>("Invalid action data schema.");
        }

        var samplePayload = WebhookSamplePayloadGenerator.Generate(actionDataSchema);

        return Result.Success(new WebhookSchemaResponse
        {
            Id = webhookEndpoint.Id,
            Endpoint = webhookEndpoint.Endpoint,
            ActionDataSchema = JsonSerializer.Deserialize<object>(webhookEndpoint.ActionDataSchema),
            SamplePayload = samplePayload
        });
    }

    public async Task<IReadOnlyList<EndpointResponse>> GetByClientTokenAsync(string clientToken, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(clientToken, cancellationToken)
            ?? throw new NotFoundException("No connection found for the supplied client token.");

        var endpoints = await endpointRepository.GetByCompanyIdAsync(connection.CompanyId, cancellationToken);
        return endpoints.Select(ToResponse).ToList();
    }

    private static EndpointResponse ToResponse(WebhookEndpoint endpoint) =>
        new(endpoint.Id,
            endpoint.EndPointToken,
            endpoint.Endpoint,
            endpoint.CompanyId,
            endpoint.TriggerConfigJson);
}
