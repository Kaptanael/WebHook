namespace MVPAPI.WebHook.Application.Common.Options;

public class WebhookRouteOptions
{
    public const string SectionName = "WebhookRoutes";
    
    // Maps event type keys to internal API URLs.
    // Example: { "door.manual": "http://localhost:7101/api/ManualDoor/Execute" }

    public Dictionary<string, string> Routes { get; set; } = [];
}
