using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Services;
using MVPAPI.WebHook.Domain.Entities;
using NSubstitute;

namespace MVPAPI.WebHook.Tests;

/// <summary>
/// Batch dispatch correctness with bounded parallelism: every claimed event is delivered and marked,
/// outcomes are counted accurately (lock-free), regardless of the configured concurrency.
/// </summary>
public class WebhookDispatchServiceTests
{
    private readonly IWebhookEventRepository _events = Substitute.For<IWebhookEventRepository>();
    private readonly IWebhookEndpointRepository _endpoints = Substitute.For<IWebhookEndpointRepository>();
    private readonly IWebHookConnectionRepository _connections = Substitute.For<IWebHookConnectionRepository>();
    private readonly IWebhookEventLifecycleService _lifecycle = Substitute.For<IWebhookEventLifecycleService>();
    private readonly IWebhookDeliveryClient _delivery = Substitute.For<IWebhookDeliveryClient>();
    private readonly ITokenDecoder _tokenDecoder = Substitute.For<ITokenDecoder>();
    private readonly IAccountApiClient _account = Substitute.For<IAccountApiClient>();

    private WebhookDispatchService Sut(int maxConcurrency) => new(
        _events, _endpoints, _connections, _lifecycle, _delivery, _tokenDecoder, _account,
        Options.Create(new WebhookDispatchOptions { MaxDeliveryConcurrency = maxConcurrency }),
        NullLogger<WebhookDispatchService>.Instance);

    [Theory]
    [InlineData(1)]   // sequential
    [InlineData(8)]   // parallel
    public async Task DispatchDueEvents_DeliversAndCountsAllOutcomes(int maxConcurrency)
    {
        var endpointId = Guid.NewGuid();
        var claimed = Enumerable.Range(0, 10)
            .Select(_ => new WebhookEvent { Id = Guid.NewGuid(), WebhookId = endpointId, EventType = "e", Payload = "{}" })
            .ToList();

        _events.ClaimDueForProcessingAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(claimed);
        _endpoints.GetByIdAsync(endpointId, Arg.Any<CancellationToken>())
            .Returns(new WebhookEndpoint { Id = endpointId, EndPointToken = "tok", Endpoint = "http://sink", CompanyId = 1 });
        _connections.GetByClientTokenAsync("tok", Arg.Any<CancellationToken>())
            .Returns(new WebHookConnection { ClientToken = "tok", IsActive = true, MVPApiToken = "t", MVPApiRefreshToken = "r" });

        // Even-indexed events succeed, odd fail — based on the event's position in the claimed list.
        var successIds = claimed.Where((_, i) => i % 2 == 0).Select(e => e.Id).ToHashSet();
        _delivery.DeliverAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>())
            .Returns(ci => successIds.Contains(ci.Arg<WebhookDelivery>().EventId)
                ? DeliveryResult.Ok()
                : DeliveryResult.Fail("boom"));

        var summary = await Sut(maxConcurrency).DispatchDueEventsAsync(50);

        Assert.Equal(10, summary.Claimed);
        Assert.Equal(5, summary.Delivered);
        Assert.Equal(5, summary.Failed);

        // Every event was terminally marked exactly once on the matching path.
        await _lifecycle.Received(5).MarkCompletedAsync(Arg.Is<Guid>(id => successIds.Contains(id)), Arg.Any<CancellationToken>());
        await _lifecycle.Received(5).MarkFailedAsync(Arg.Is<Guid>(id => !successIds.Contains(id)), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DispatchDueEvents_NoClaims_ReturnsEmptySummary_AndDeliversNothing()
    {
        _events.ClaimDueForProcessingAsync(Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WebhookEvent>());

        var summary = await Sut(8).DispatchDueEventsAsync(50);

        Assert.Equal(new DispatchSummary(0, 0, 0), summary);
        await _delivery.DidNotReceive().DeliverAsync(Arg.Any<WebhookDelivery>(), Arg.Any<CancellationToken>());
    }
}
