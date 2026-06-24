namespace MVPAPI.WebHook.Application.Common;

public class WebhookDispatchOptions
{
    public const string SectionName = "WebhookDispatch";

    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int DeliveryTimeoutSeconds { get; set; } = 30;
    public int StaleClaimTimeoutSeconds { get; set; } = 300;

    /// <summary>Max number of deliveries within a batch that run concurrently. A slow endpoint then
    /// can't stall the whole batch. Clamped to at least 1.</summary>
    public int MaxDeliveryConcurrency { get; set; } = 8;

    /// <summary>Attempts before an event is marked permanently Failed.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Base unit of the exponential retry backoff: delay grows as base * 2^(attempt-1).</summary>
    public int RetryBackoffBaseSeconds { get; set; } = 60;

    /// <summary>Upper bound on the (pre-jitter) retry backoff.</summary>
    public int RetryBackoffCapSeconds { get; set; } = 3600;
}
