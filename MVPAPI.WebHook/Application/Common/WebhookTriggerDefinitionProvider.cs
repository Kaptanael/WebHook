using MVPAPI.WebHook.Application.DTOs.TriggerDefinitions;

namespace MVPAPI.WebHook.Application.Common;

public static class WebhookTriggerDefinitionProvider
{
    public static IReadOnlyList<WebhookTriggerDefinition> GetAll()
    {
        return new List<WebhookTriggerDefinition>
        {
            new()
            {
                TriggerType = WebhookTriggerTypes.AccessEvent,
                Name = "Access Event",
                Description = "Fires when access is granted or denied.",
                AllowedFilters = new[]
                {
                    "companyId",
                    "location",
                    "deviceType",
                    "panelNo",
                    "readerNo",
                    "status",
                    "badge",
                    "facilityNo"
                },
                SampleTriggerConfigJson = new SampleTriggerConfigJson
                {
                    TriggerType = WebhookTriggerTypes.AccessEvent,
                    CompanyId = 1001,
                    Filters = new Dictionary<string, object?>
                    {
                        ["location"] = "Dhaka Office",
                        ["deviceType"] = 1,
                        ["panelNo"] = 2,
                        ["readerNo"] = 5,
                        ["status"] = 10,
                        ["badge"] = "123456789",
                        ["facilityNo"] = 101
                    }
                },
                SamplePayload = new Dictionary<string, object?>
                {
                    ["eventId"] = Guid.NewGuid(),
                    ["eventType"] = "access.granted",
                    ["eventTimeUtc"] = DateTime.UtcNow,
                    ["companyId"] = 1001,
                    ["location"] = "Dhaka Office",
                    ["deviceType"] = 1,
                    ["panelNo"] = 2,
                    ["readerNo"] = 5,
                    ["status"] = 10,
                    ["statusText"] = "Access Granted",
                    ["badge"] = "123456789",
                    ["facilityNo"] = 101
                }
            },

            new()
            {
                TriggerType = WebhookTriggerTypes.InputEvent,
                Name = "Input Event",
                Description = "Fires when an input device changes status.",
                AllowedFilters = new[]
                {
                    "companyId",
                    "location",
                    "deviceType",
                    "panelNo",
                    "inputNo",
                    "status"
                },
                SampleTriggerConfigJson = new SampleTriggerConfigJson
                {
                    TriggerType = WebhookTriggerTypes.InputEvent,
                    CompanyId = 1001,
                    Filters = new Dictionary<string, object?>
                    {
                        ["location"] = "Dhaka Office",
                        ["deviceType"] = 2,
                        ["panelNo"] = 2,
                        ["inputNo"] = 3,
                        ["status"] = 20
                    }
                },
                SamplePayload = new Dictionary<string, object?>
                {
                    ["eventId"] = Guid.NewGuid(),
                    ["eventType"] = "input.alarm",
                    ["eventTimeUtc"] = DateTime.UtcNow,
                    ["companyId"] = 1001,
                    ["location"] = "Dhaka Office",
                    ["deviceType"] = 2,
                    ["panelNo"] = 2,
                    ["inputNo"] = 3,
                    ["status"] = 20,
                    ["statusText"] = "Input Alarm"
                }
            },

            new()
            {
                TriggerType = WebhookTriggerTypes.RelayEvent,
                Name = "Relay Event",
                Description = "Fires when a relay is activated or deactivated.",
                AllowedFilters = new[]
                {
                    "companyId",
                    "location",
                    "deviceType",
                    "panelNo",
                    "relayNo",
                    "status"
                },
                SampleTriggerConfigJson = new SampleTriggerConfigJson
                {
                    TriggerType = WebhookTriggerTypes.RelayEvent,
                    CompanyId = 1001,
                    Filters = new Dictionary<string, object?>
                    {
                        ["location"] = "Dhaka Office",
                        ["deviceType"] = 3,
                        ["panelNo"] = 2,
                        ["relayNo"] = 1,
                        ["status"] = 30
                    }
                },
                SamplePayload = new Dictionary<string, object?>
                {
                    ["eventId"] = Guid.NewGuid(),
                    ["eventType"] = "relay.activated",
                    ["eventTimeUtc"] = DateTime.UtcNow,
                    ["companyId"] = 1001,
                    ["location"] = "Dhaka Office",
                    ["deviceType"] = 3,
                    ["panelNo"] = 2,
                    ["relayNo"] = 1,
                    ["status"] = 30,
                    ["statusText"] = "Relay Activated"
                }
            }
        };
    }
}
