namespace MVPAPI.WebHook.Application.Common;

public class WebhookRouteOptions
{
    public const string SectionName = "WebhookRoutes";

    /// <summary>
    /// Maps event type keys to internal API URLs.
    /// Example: { "event.door.manual": "http://localhost:7101/api/ManualDoor/Execute" }
    /// </summary>
    public Dictionary<string, string> Routes { get; set; } = [];
}
