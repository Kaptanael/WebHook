using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Domain.Entities;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Passthrough adapter: the request body is taken as the event payload verbatim. The event type is
/// read from the Standard Webhooks envelope's top-level <c>type</c> field, falling back to the
/// <c>X-Event-Type</c> header when the body carries no <c>type</c> (so a sender can authenticate with
/// only the <c>x-token</c> header and no other headers). The matched endpoint's URL is the provider.
/// </summary>
public class DefaultPayloadAdapter : IPayloadAdapter
{
    public const string EventTypeHeader = "X-Event-Type";

    public Result<NormalizedInboundEvent> Normalize(InboundRequest request, WebhookEndpoint endpoint)
    {
        if (string.IsNullOrWhiteSpace(request.RawBody))
            return Result.Failure<NormalizedInboundEvent>("Payload cannot be empty.");

        // Prefer the envelope's "type"; fall back to the X-Event-Type header.
        var eventType = ReadEventTypeFromBody(request.RawBody);
        if (string.IsNullOrWhiteSpace(eventType) &&
            request.Headers.TryGetValue(EventTypeHeader, out var headerType))
            eventType = headerType;

        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<NormalizedInboundEvent>(
                $"Event type not found: include a \"type\" field in the body or the {EventTypeHeader} header.");

        return Result.Success(new NormalizedInboundEvent(eventType.Trim(), request.RawBody, endpoint.Endpoint));
    }

    /// <summary>Returns the top-level <c>type</c> string from a JSON body, or null if absent/not JSON.</summary>
    private static string? ReadEventTypeFromBody(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("type", out var typeElement) &&
                typeElement.ValueKind == JsonValueKind.String)
                return typeElement.GetString();
        }
        catch (JsonException)
        {
            // Body is not JSON (or malformed) — let the header fallback handle it.
        }

        return null;
    }
}
