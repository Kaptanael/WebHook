using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Passthrough adapter: the request body is taken as the event payload verbatim and the event type
/// is read from the <c>X-Event-Type</c> header. The matched endpoint's URL is recorded as the provider.
/// </summary>
public class DefaultPayloadAdapter : IPayloadAdapter
{
    public const string EventTypeHeader = "X-Event-Type";

    public Result<NormalizedInboundEvent> Normalize(InboundRequest request, WebhookEndpoint endpoint)
    {
        if (!request.Headers.TryGetValue(EventTypeHeader, out var eventType) || string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<NormalizedInboundEvent>($"Missing {EventTypeHeader} header.");

        if (string.IsNullOrWhiteSpace(request.RawBody))
            return Result.Failure<NormalizedInboundEvent>("Payload cannot be empty.");

        return Result.Success(new NormalizedInboundEvent(eventType.Trim(), request.RawBody, endpoint.Endpoint));
    }
}
