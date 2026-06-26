using MVPAPI.WebHook.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.DTOs.Outbounds;

public record SubscribeRequest(string Endpoint, JsonElement TriggerConfigJson);

public class SubscribeResponse
{
    public Guid Id { get; set; }

    public string RemoteEndpoint { get; set; } = string.Empty;

    public string ResponseSchemaUrl { get; set; } = string.Empty;
}

public record UnsubscribeResponse(Guid SubscriberId);

public record WebhookOutboundResponse(
    Guid Id,
    string EndpointToken,
    string Endpoint,
    int CompanyId,
    string? TriggerJson);

public class TriggerConfig
{
    [Required]
    public string TriggerType { get; set; } = string.Empty;

    [Required]
    public int CompanyId { get; set; }

    [Required]
    public DeviceType DeviceType { get; set; }

    [Required]
    public Guid PanelId { get; set; }

    [Required]
    public Guid DeviceId { get; set; }

    public int? Status { get; set; }

    public long? Badge { get; set; }

    public int? FacilityNo { get; set; }
}

public sealed class WebhookSchemaResponse
{
    public Guid Id { get; set; }

    public string Endpoint { get; set; } = string.Empty;

    public object? ActionDataSchema { get; set; }

    public object? SamplePayload { get; set; }
}


