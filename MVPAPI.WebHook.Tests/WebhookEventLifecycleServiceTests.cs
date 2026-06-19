using Microsoft.Extensions.Logging;
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
    [InlineData(0, 1)]   // 1st failure  -> attempt 1 -> 2^0 = 1 minute
    [InlineData(1, 2)]   // 2nd failure  -> attempt 2 -> 2^1 = 2 minutes
    [InlineData(2, 4)]   // 3rd failure  -> attempt 3 -> 2^2 = 4 minutes
    [InlineData(3, 8)]   // 4th failure  -> attempt 4 -> 2^3 = 8 minutes
    public async Task MarkFailed_BelowMax_SetsRetryingWithExponentialBackoff(
        int startingAttempts, double expectedMinutes)
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
        Assert.InRange(
            webhookEvent.NextAttemptAtUtc!.Value,
            before.AddMinutes(expectedMinutes),
            after.AddMinutes(expectedMinutes));
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
