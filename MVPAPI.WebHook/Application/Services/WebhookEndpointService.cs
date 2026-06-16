using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Endpoints;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services;

public class WebhookEndpointService(
    ITokenValidator tokenValidator,
    IWebHookConnectionRepository connectionRepository,
    IWebhookEndpointRepository endpointRepository) : IWebhookEndpointService
{
    public async Task<Result<SubscribeResponse>> SubscribeAsync(string token, SubscribeRequest request, CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenValidator.DecodeToken(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<SubscribeResponse>(decodeResult.Error!);

        var companyId = decodeResult.Value!.CompanyId;

        var existingWebhookEndpoint = await endpointRepository.GetByEndpointAsync(request.Endpoint, cancellationToken);
        if (existingWebhookEndpoint is not null)
            return Result.Failure<SubscribeResponse>("Webhook endpoint already exists.");

        var triggerConfigJson = request.TriggerConfigJson.GetRawText();

        var schemaResult = WebhookSchemaGenerator.Generate(triggerConfigJson);
        if (!schemaResult.IsSuccess)
            return Result<SubscribeResponse>.Failure(schemaResult.Error!);

        var actionDataSchema = schemaResult.Value!;

        var endpoint = new WebhookEndpoint
        {
            EndPointToken = token,
            Endpoint = request.Endpoint,
            CompanyId = companyId,
            TriggerConfigJson = triggerConfigJson,
            ActionDataSchema = JsonSerializer.Serialize(actionDataSchema)
        };

        await endpointRepository.AddAsync(endpoint, cancellationToken);

        return Result.Success(new SubscribeResponse
        {
            Id = endpoint.Id,
            Endpoint = endpoint.Endpoint            
        });
    }

    public async Task<Result<UnsubscribeResponse>> UnsubscribeAsync(string token, Guid subscriberId, CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenValidator.DecodeToken(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<UnsubscribeResponse>(decodeResult.Error!);

        var companyId = decodeResult.Value!.CompanyId;

        var endpoint = await endpointRepository.GetByIdAsync(subscriberId, cancellationToken);
        if (endpoint is null)
            return Result.Failure<UnsubscribeResponse>("Webhook endpoint not found.");

        if (endpoint.CompanyId != companyId)
            return Result.Failure<UnsubscribeResponse>("Webhook endpoint does not belong to this company.");

        var removed = await endpointRepository.DeleteAsync(endpoint.Id, cancellationToken);
        return removed
            ? Result.Success(new UnsubscribeResponse(subscriberId))
            : Result.Failure<UnsubscribeResponse>("Failed to remove webhook endpoint.");
    }

    public async Task<Result<WebhookSchemaResponse>> GetSchemaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var webhookEndpoint = await endpointRepository.GetByIdAsync(id, cancellationToken);

        if (webhookEndpoint == null)
            return Result.Failure<WebhookSchemaResponse>("Webhook endpoint not found.");

        //var triggerConfig = JsonSerializer.Deserialize<TriggerConfig>(webhookEndpoint.TriggerConfigJson);
        //if (triggerConfig == null)
        //    return Result.Failure<WebhookSchemaResponse>("Invalid trigger configuration."); 

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
