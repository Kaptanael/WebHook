namespace MVPAPI.WebHook.Application.Common;

public class WebhookDispatchOptions
{
    public const string SectionName = "WebhookDispatch";

    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int DeliveryTimeoutSeconds { get; set; } = 30;
    public int StaleClaimTimeoutSeconds { get; set; } = 300;
}
