namespace MVPAPI.WebHook.Application.Common;

public static class WebhookTriggerTypes
{
    public const string AccessEvent = "access.event";
    public const string InputEvent = "input.event";
    public const string RelayEvent = "relay.event";
    public const string DeviceStatus = "device.status";
}

public sealed class WebhookFieldSchema
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; }
    public object? Example { get; set; }
}

public sealed class WebhookActionDataSchema
{
    public string TriggerType { get; set; } = string.Empty;
    public string PayloadName { get; set; } = string.Empty;
    public List<WebhookFieldSchema> Fields { get; set; } = new();
}

public static class WebhookSamplePayloadGenerator
{
    public static Dictionary<string, object?> Generate(WebhookActionDataSchema schema)
    {
        return schema.Fields.ToDictionary(
            field => field.Name,
            field => field.Example
        );
    }
}