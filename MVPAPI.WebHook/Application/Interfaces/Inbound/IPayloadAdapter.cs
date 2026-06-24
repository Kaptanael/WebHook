using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

/// <summary>
/// Normalizes a foreign provider payload into the internal event shape for the matched endpoint.
/// </summary>
public interface IPayloadAdapter
{
    Result<NormalizedInboundEvent> Normalize(InboundRequest request, WebhookEndpoint endpoint);
}
