namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

/// <summary>
/// Orchestrates an inbound webhook: pick the auth strategy by header → authenticate and resolve the
/// endpoint → verify the tenant connection → normalize the payload → queue the event.
/// </summary>
public interface IInboundWebhookHandler
{
    Task<InboundResult> HandleAsync(InboundRequest request, CancellationToken cancellationToken = default);
}
