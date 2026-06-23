using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Inbound;

/// <summary>
/// Authenticates an inbound request against a candidate <see cref="WebhookEndpoint"/> using one
/// mechanism (Standard Webhooks signature, API key, custom token). The pipeline tries every
/// authenticator against each active endpoint, so a method that doesn't apply (its header is absent)
/// simply fails and the next is tried — no per-endpoint method selector is needed.
/// </summary>
public interface IInboundAuthenticator
{
    Result Authenticate(InboundRequest request, WebhookEndpoint endpoint);
}
