namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

public record InboundRequest(
    IReadOnlyDictionary<string, string> Headers,
    string RawBody);

public record NormalizedInboundEvent(string EventType, string Payload);

public enum InboundOutcome
{    
    Accepted,    
    Unauthorized,    
    Invalid
}

public sealed record InboundResult(InboundOutcome Outcome, string? Error = null, int QueuedCount = 0)
{
    public static InboundResult Accepted(int queuedCount) => new(InboundOutcome.Accepted, QueuedCount: queuedCount);
    public static InboundResult Unauthorized(string error) => new(InboundOutcome.Unauthorized, error);
    public static InboundResult Invalid(string error) => new(InboundOutcome.Invalid, error);
}
