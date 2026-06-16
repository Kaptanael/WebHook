namespace MVPAPI.WebHook.Application.DTOs.TriggerDefinitions;

public sealed class WebhookTriggerDefinition
{
    public string TriggerType { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IReadOnlyList<string> AllowedFilters { get; set; } = Array.Empty<string>();

    public SampleTriggerConfigJson SampleTriggerConfigJson { get; set; } = new();

    public IReadOnlyDictionary<string, object?> SamplePayload { get; set; }
        = new Dictionary<string, object?>();
}

public sealed class SampleTriggerConfigJson
{
    public string TriggerType { get; set; } = string.Empty;

    public int CompanyId { get; set; }

    public IReadOnlyDictionary<string, object?> Filters { get; set; }
        = new Dictionary<string, object?>();
}