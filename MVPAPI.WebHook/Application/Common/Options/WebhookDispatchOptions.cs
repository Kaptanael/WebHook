namespace MVPAPI.WebHook.Application.Common.Options;

public class WebhookDispatchOptions
{
    public const string SectionName = "WebhookDispatch";

    public int BatchSize { get; set; } = 50;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int DeliveryTimeoutSeconds { get; set; } = 30;
    public int StaleClaimTimeoutSeconds { get; set; } = 300;

    // Max number of deliveries within a batch that run concurrently. A slow endpoint then
    // can't stall the whole batch. Clamped to at least 1.
    public int MaxDeliveryConcurrency { get; set; } = 8;

    // Attempts before an event is marked permanently Failed.
    public int MaxAttempts { get; set; } = 5;

    // Base unit of the exponential retry backoff: delay grows as base * 2^(attempt-1).
    public int RetryBackoffBaseSeconds { get; set; } = 60;

    // Upper bound on the (pre-jitter) retry backoff.
    public int RetryBackoffCapSeconds { get; set; } = 3600;
}
