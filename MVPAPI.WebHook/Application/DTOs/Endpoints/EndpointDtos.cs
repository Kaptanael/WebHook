using System.Text.Json;

namespace MVPAPI.WebHook.Application.DTOs.Endpoints;

public record SubscribeRequest(string Endpoint, JsonElement TriggerConfigJson);

public class SubscribeResponse
{    
    public Guid Id { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public string ResponseSchemaUrl { get; set; } = string.Empty;
}

public record UnsubscribeResponse(Guid SubscriberId);

public record EndpointResponse(
    Guid Id,
    string EndpointToken,
    string Endpoint,
    int CompanyId,
    string? TriggerJson);

public record FieldMapping(string SourceField, string TargetField);

public class TriggerConfig
{
    public int CompanyId { get; set; }    

    public int? DeviceType { get; set; }

    public int? PanelNo { get; set; }

    public int? DeviceNo { get; set; }

    public int? Status { get; set; }

    public string? Badge { get; set; }

    public int? FacilityNo { get; set; }
}

public sealed class WebhookSchemaResponse
{
    public Guid Id { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public object? ActionDataSchema { get; set; }

    public object? SamplePayload { get; set; }
}


