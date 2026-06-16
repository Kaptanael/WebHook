using MVPAPI.WebHook.Application.Common;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Common;

public static class WebhookSchemaGenerator
{
    public static Result<WebhookActionDataSchema> Generate(string triggerConfigJson)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(triggerConfigJson);
        }
        catch
        {
            return Result<WebhookActionDataSchema>.Failure("Invalid TriggerConfigJson.");
        }

        var root = document.RootElement;

        if (!root.TryGetProperty("triggerType", out var triggerTypeElement))
            return Result<WebhookActionDataSchema>.Failure("triggerType is required.");

        var triggerType = triggerTypeElement.GetString();

        if (string.IsNullOrWhiteSpace(triggerType))
            return Result<WebhookActionDataSchema>.Failure("triggerType is required.");

        return triggerType switch
        {
            WebhookTriggerTypes.AccessEvent => Result<WebhookActionDataSchema>.Success(BuildAccessEventSchema(root)),
            WebhookTriggerTypes.InputEvent => Result<WebhookActionDataSchema>.Success(BuildInputEventSchema(root)),
            WebhookTriggerTypes.RelayEvent => Result<WebhookActionDataSchema>.Success(BuildRelayEventSchema(root)),
            WebhookTriggerTypes.DeviceStatus => Result<WebhookActionDataSchema>.Success(BuildDeviceStatusSchema(root)),

            _ => Result<WebhookActionDataSchema>.Failure($"Unsupported triggerType: {triggerType}")
        };
    }

    private static WebhookActionDataSchema BuildAccessEventSchema(JsonElement root)
    {
        return new WebhookActionDataSchema
        {
            TriggerType = WebhookTriggerTypes.AccessEvent,
            PayloadName = "Access Event Payload",
            Fields =
            {
                Field("eventId", "guid", true, Guid.NewGuid()),
                Field("eventType", "string", true, "access.granted"),
                Field("eventTimeUtc", "datetime", true, DateTime.UtcNow.ToString("O")),
                Field("companyId", "integer", true, GetCompanyId(root)),
                Field("location", "string", false, "Dhaka Office"),
                Field("deviceType", "integer", true, 1),
                Field("panelNo", "integer", true, 2),
                Field("readerNo", "integer", false, 5),
                Field("status", "integer", true, 10),
                Field("statusText", "string", true, "Access Granted"),
                Field("badge", "string", false, "123456789"),
                Field("facilityNo", "integer", false, 101)
            }
        };
    }

    private static WebhookActionDataSchema BuildInputEventSchema(JsonElement root)
    {
        return new WebhookActionDataSchema
        {
            TriggerType = WebhookTriggerTypes.InputEvent,
            PayloadName = "Input Event Payload",
            Fields =
            {
                Field("eventId", "guid", true, Guid.NewGuid()),
                Field("eventType", "string", true, "input.alarm"),
                Field("eventTimeUtc", "datetime", true, DateTime.UtcNow.ToString("O")),
                Field("companyId", "integer", true, GetCompanyId(root)),
                Field("location", "string", false, "Dhaka Office"),
                Field("deviceType", "integer", true, 2),
                Field("panelNo", "integer", true, 2),
                Field("inputNo", "integer", true, 3),
                Field("status", "integer", true, 20),
                Field("statusText", "string", true, "Input Alarm")
            }
        };
    }

    private static WebhookActionDataSchema BuildRelayEventSchema(JsonElement root)
    {
        return new WebhookActionDataSchema
        {
            TriggerType = WebhookTriggerTypes.RelayEvent,
            PayloadName = "Relay Event Payload",
            Fields =
            {
                Field("eventId", "guid", true, Guid.NewGuid()),
                Field("eventType", "string", true, "relay.activated"),
                Field("eventTimeUtc", "datetime", true, DateTime.UtcNow.ToString("O")),
                Field("companyId", "integer", true, GetCompanyId(root)),
                Field("location", "string", false, "Dhaka Office"),
                Field("deviceType", "integer", true, 3),
                Field("panelNo", "integer", true, 2),
                Field("relayNo", "integer", true, 1),
                Field("status", "integer", true, 30),
                Field("statusText", "string", true, "Relay Activated")
            }
        };
    }

    private static WebhookActionDataSchema BuildDeviceStatusSchema(JsonElement root)
    {
        return new WebhookActionDataSchema
        {
            TriggerType = WebhookTriggerTypes.DeviceStatus,
            PayloadName = "Device Status Payload",
            Fields =
            {
                Field("eventId", "guid", true, Guid.NewGuid()),
                Field("eventType", "string", true, "device.status"),
                Field("eventTimeUtc", "datetime", true, DateTime.UtcNow.ToString("O")),
                Field("companyId", "integer", true, GetCompanyId(root)),
                Field("location", "string", false, "Dhaka Office"),
                Field("deviceType", "integer", true, 1),
                Field("panelNo", "integer", true, 2),
                Field("deviceNo", "integer", true, 5),
                Field("status", "integer", true, 1),
                Field("statusText", "string", true, "Online")
            }
        };
    }

    private static WebhookFieldSchema Field(
        string name,
        string type,
        bool required,
        object? example)
    {
        return new WebhookFieldSchema
        {
            Name = name,
            Type = type,
            Required = required,
            Example = example
        };
    }

    private static int GetCompanyId(JsonElement root)
    {
        if (root.TryGetProperty("companyId", out var companyIdElement))
            return companyIdElement.GetInt32();

        return 0;
    }
}