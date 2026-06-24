using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Services;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Domain.Enums;
using NSubstitute;

namespace MVPAPI.WebHook.Tests;

public class WebhookEventLifecycleServiceTests
{
    private readonly IWebhookEventRepository _repo = Substitute.For<IWebhookEventRepository>();
    private readonly WebhookEventLifecycleService _sut;

    public WebhookEventLifecycleServiceTests()
    {
        _sut = new WebhookEventLifecycleService(
            _repo,
            Options.Create(new WebhookDispatchOptions()),
            Substitute.For<ILogger<WebhookEventLifecycleService>>());
    }

    private WebhookEvent ArrangeExisting(WebhookEvent webhookEvent)
    {
        _repo.GetByIdAsync(webhookEvent.Id, Arg.Any<CancellationToken>()).Returns(webhookEvent);
        return webhookEvent;
    }

    private void ArrangeMissing()
        => _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookEvent?)null);

    [Fact]
    public async Task MarkProcessing_NotFound_ReturnsFalseAndDoesNotUpdate()
    {
        ArrangeMissing();

        var result = await _sut.MarkProcessingAsync(Guid.NewGuid());

        Assert.False(result);
        await _repo.DidNotReceive().UpdateAsync(Arg.Any<WebhookEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkProcessing_SetsProcessingStateAndStartTime()
    {
        var webhookEvent = ArrangeExisting(new WebhookEvent { Id = Guid.NewGuid(), Status = EventStatus.Pending });

        var result = await _sut.MarkProcessingAsync(webhookEvent.Id);

        Assert.True(result);
        Assert.Equal(EventStatus.Processing, webhookEvent.Status);
        Assert.NotNull(webhookEvent.ProcessingStartedAtUtc);
        await _repo.Received(1).UpdateAsync(webhookEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkCompleted_SetsCompletedAndClearsNextAttempt()
    {
        var webhookEvent = ArrangeExisting(new WebhookEvent
        {
            Id = Guid.NewGuid(),
            Status = EventStatus.Processing,
            NextAttemptAtUtc = DateTime.UtcNow
        });

        var result = await _sut.MarkCompletedAsync(webhookEvent.Id);

        Assert.True(result);
        Assert.Equal(EventStatus.Completed, webhookEvent.Status);
        Assert.NotNull(webhookEvent.ProcessedAtUtc);
        Assert.Null(webhookEvent.NextAttemptAtUtc);
    }

    [Theory]
    [InlineData(0, 60)]    // attempt 1 -> base 60 * 2^0 = 60s,  jittered to [30, 60]
    [InlineData(1, 120)]   // attempt 2 -> 120s, jittered to [60, 120]
    [InlineData(2, 240)]   // attempt 3 -> 240s, jittered to [120, 240]
    [InlineData(3, 480)]   // attempt 4 -> 480s, jittered to [240, 480]
    public async Task MarkFailed_BelowMax_SetsRetryingWithJitteredExponentialBackoff(
        int startingAttempts, double baseSeconds)
    {
        var webhookEvent = ArrangeExisting(new WebhookEvent
        {
            Id = Guid.NewGuid(),
            Attempts = startingAttempts,
            Status = EventStatus.Processing
        });

        var before = DateTime.UtcNow;
        var result = await _sut.MarkFailedAsync(webhookEvent.Id, "boom");
        var after = DateTime.UtcNow;

        Assert.True(result);
        Assert.Equal(EventStatus.Retrying, webhookEvent.Status);
        Assert.Equal(startingAttempts + 1, webhookEvent.Attempts);
        Assert.Equal("boom", webhookEvent.LastError);
        Assert.NotNull(webhookEvent.NextAttemptAtUtc);
        // Equal jitter: delay is between base/2 and base.
        Assert.InRange(
            webhookEvent.NextAttemptAtUtc!.Value,
            before.AddSeconds(baseSeconds / 2.0),
            after.AddSeconds(baseSeconds));
    }

    [Fact]
    public async Task MarkFailed_AppliesJitter_DelaysVaryAcrossCalls()
    {
        var offsets = new HashSet<long>();
        for (var i = 0; i < 8; i++)
        {
            var e = ArrangeExisting(new WebhookEvent { Id = Guid.NewGuid(), Attempts = 3, Status = EventStatus.Processing });
            var before = DateTime.UtcNow;
            await _sut.MarkFailedAsync(e.Id, "x");
            offsets.Add((long)(e.NextAttemptAtUtc!.Value - before).TotalMilliseconds);
        }

        Assert.True(offsets.Count > 1, "jitter should produce varying retry delays");
    }

    [Fact]
    public async Task MarkFailed_ReachingMaxAttempts_SetsFailedAndClearsNextAttempt()
    {
        // Starts at 4; this failure makes it 5 (MaxAttempts), which is terminal.
        var webhookEvent = ArrangeExisting(new WebhookEvent
        {
            Id = Guid.NewGuid(),
            Attempts = 4,
            Status = EventStatus.Processing
        });

        var result = await _sut.MarkFailedAsync(webhookEvent.Id, "final boom");

        Assert.True(result);
        Assert.Equal(5, webhookEvent.Attempts);
        Assert.Equal(EventStatus.Failed, webhookEvent.Status);
        Assert.Null(webhookEvent.NextAttemptAtUtc);
        Assert.Equal("final boom", webhookEvent.LastError);
    }

    [Fact]
    public async Task MarkFailed_NotFound_ReturnsFalse()
    {
        ArrangeMissing();

        var result = await _sut.MarkFailedAsync(Guid.NewGuid(), "x");

        Assert.False(result);
    }
}
