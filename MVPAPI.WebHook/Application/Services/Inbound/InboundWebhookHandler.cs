using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class InboundWebhookHandler(
StandardWebhookReceiver standardWebhookReceiver,
ApiKeyReceiver apiKeyReceiver,
TokenWebhookReceiver tokenReceiver) : IInboundWebhookHandler
{
    public Task<InboundResult> HandleAsync(InboundRequest request, CancellationToken cancellationToken = default)
    {
        var headers = request.Headers;
        var hasApiKey = Present(headers, WebhookHeaders.ApiKeyHeader);
        var hasSignatureTriplet = Present(headers, WebhookHeaders.IdHeader) 
            && Present(headers, WebhookHeaders.TimestampHeader) 
            && Present(headers, WebhookHeaders.SignatureHeader);

        if (Present(headers, WebhookHeaders.TokenHeader))
            return tokenReceiver.HandleAsync(request, cancellationToken);

        if (hasApiKey && hasSignatureTriplet)
            return standardWebhookReceiver.HandleAsync(request, cancellationToken);

        if (hasApiKey)
            return apiKeyReceiver.HandleAsync(request, cancellationToken);        

        return Task.FromResult(InboundResult.Invalid("Missing credential headers."));
    }

    private static bool Present(IReadOnlyDictionary<string, string> headers, string key) =>
        headers.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);
}
