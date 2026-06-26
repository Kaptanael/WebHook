namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

/// <summary>A raw inbound webhook request, decoupled from ASP.NET so strategies are testable.</summary>
public record InboundRequest(
    IReadOnlyDictionary<string, string> Headers,
    string RawBody);

/// <summary>The canonical internal event an adapter normalizes a foreign payload into.</summary>
public record NormalizedInboundEvent(
    string EventType,
    string Payload,
    string? Provider);

public enum InboundOutcome
{
    /// <summary>Authenticated, normalized, and queued.</summary>
    Accepted,
    /// <summary>No active integration's credentials matched the request. (Also covers "unknown" — header-based
    /// resolution can't distinguish a bad credential from an unconfigured sender without leaking which keys exist.)</summary>
    Unauthorized,
    /// <summary>Misconfiguration or a payload that could not be normalized.</summary>
    Invalid
}

public sealed record InboundResult(InboundOutcome Outcome, string? Error = null, int QueuedCount = 0)
{
    public static InboundResult Accepted(int queuedCount) => new(InboundOutcome.Accepted, QueuedCount: queuedCount);
    public static InboundResult Unauthorized(string error) => new(InboundOutcome.Unauthorized, error);
    public static InboundResult Invalid(string error) => new(InboundOutcome.Invalid, error);
}

/// <summary>
/// Orchestrates the inbound pipeline: resolve integration from credential headers → normalize → queue.
/// </summary>
public interface IInboundWebhookPipeline
{
    Task<InboundResult> ProcessAsync(InboundRequest request, CancellationToken cancellationToken = default);
}
