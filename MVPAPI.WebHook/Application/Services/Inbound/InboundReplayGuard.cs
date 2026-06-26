using Microsoft.Extensions.Caching.Memory;

namespace MVPAPI.WebHook.Application.Services.Inbound;

/// <summary>
/// Dedupes inbound Standard Webhooks requests by their <c>webhook-id</c> so a captured, validly-signed
/// request cannot be replayed within the timestamp-tolerance window. An id is recorded only after the
/// event is successfully queued, so a provider that retries a genuinely-failed delivery (same id) is not
/// mistaken for a replay.
/// </summary>
public interface IInboundReplayGuard
{
    /// <summary>True when this <paramref name="webhookId"/> has already been processed for the scope.</summary>
    bool AlreadyProcessed(string scope, string webhookId);

    /// <summary>Records this <paramref name="webhookId"/> as processed for the scope.</summary>
    void MarkProcessed(string scope, string webhookId);
}

public class InboundReplayGuard(IMemoryCache cache) : IInboundReplayGuard
{
    // Cover the full ±300s timestamp tolerance the resolver enforces (2× to be safe), after which an
    // expired-timestamp replay is already rejected up front and the id no longer needs remembering.
    private static readonly TimeSpan Retention = TimeSpan.FromSeconds(600);

    public bool AlreadyProcessed(string scope, string webhookId) =>
        cache.TryGetValue(Key(scope, webhookId), out _);

    public void MarkProcessed(string scope, string webhookId) =>
        cache.Set(Key(scope, webhookId), true, Retention);

    private static string Key(string scope, string webhookId) => $"replay:{scope}:{webhookId}";
}
