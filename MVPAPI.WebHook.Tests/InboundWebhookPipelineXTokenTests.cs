using Microsoft.Extensions.Logging.Abstractions;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;
using NSubstitute;

namespace MVPAPI.WebHook.Tests;

/// <summary>
/// The x-token path: a request authenticates with only the <c>x-token</c> header — no
/// <c>webhook-timestamp</c>/<c>webhook-signature</c>/<c>x-api-key</c>, and no <c>X-Event-Type</c> when the
/// body carries a top-level <c>type</c>. Uses the real <see cref="TokenInboundAuthenticator"/> and
/// <see cref="DefaultPayloadAdapter"/>; the rest is substituted.
/// </summary>
public class InboundWebhookPipelineXTokenTests
{
    private const string Token = "endpoint-token-abc";

    private readonly IWebhookEndpointRepository _endpoints = Substitute.For<IWebhookEndpointRepository>();
    private readonly IWebHookConnectionRepository _connections = Substitute.For<IWebHookConnectionRepository>();
    private readonly IApiKeyInboundResolver _apiKeyResolver = Substitute.For<IApiKeyInboundResolver>();
    private readonly IInboundProvisioningService _provisioning = Substitute.For<IInboundProvisioningService>();
    private readonly IWebhookEventService _eventService = Substitute.For<IWebhookEventService>();
    private readonly ITokenDecoder _tokenDecoder = Substitute.For<ITokenDecoder>();

    private readonly WebhookEndpoint _endpoint = new()
    {
        Id = Guid.NewGuid(),
        EndPointToken = Token,
        Endpoint = "http://internal/api/ManualDoor/Execute",
        CompanyId = 500646,
        IsActive = true,
    };

    private InboundWebhookPipeline Sut() => new(
        _endpoints,
        _connections,
        _apiKeyResolver,
        _provisioning,
        [new TokenInboundAuthenticator()],   // real authenticator
        new DefaultPayloadAdapter(),         // real adapter
        _eventService,
        _tokenDecoder,
        NullLogger<InboundWebhookPipeline>.Instance);

    public InboundWebhookPipelineXTokenTests()
    {
        // No x-api-key presented → the api-key resolver does not apply.
        _apiKeyResolver.ResolveAsync(Arg.Any<InboundRequest>(), Arg.Any<CancellationToken>())
            .Returns(ApiKeyAuthResult.NotPresented());
        _endpoints.GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { _endpoint });
        _connections.GetByClientTokenAsync(Token, Arg.Any<CancellationToken>())
            .Returns(new WebHookConnection { ClientToken = Token, CompanyId = 500646, IsActive = true });
    }

    private static InboundRequest Request(string body, params (string Key, string Value)[] headers) =>
        new(headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase), body);

    [Fact]
    public async Task XTokenOnly_WithTypeInBody_IsAcceptedAndQueued()
    {
        // Only x-token. No timestamp, signature, api-key, or X-Event-Type — the body's "type" sets the event.
        var body = """{"type":"door.manual","companyId":500646,"doorIds":["d1"]}""";
        var request = Request(body, ("x-token", Token));

        var result = await Sut().ProcessAsync(request);

        Assert.Equal(InboundOutcome.Accepted, result.Outcome);
        Assert.Equal(1, result.QueuedCount);
        await _eventService.Received(1).PublishToEndpointAsync(
            _endpoint, "door.manual", body, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task XTokenOnly_TypeFromHeaderWhenBodyHasNone_IsAccepted()
    {
        // Body has no "type"; the X-Event-Type header still works as the fallback.
        var body = """{"companyId":500646,"doorIds":["d1"]}""";
        var request = Request(body, ("x-token", Token), ("X-Event-Type", "door.manual"));

        var result = await Sut().ProcessAsync(request);

        Assert.Equal(InboundOutcome.Accepted, result.Outcome);
        await _eventService.Received(1).PublishToEndpointAsync(
            _endpoint, "door.manual", body, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WrongToken_DoesNotMatchAnyEndpoint_IsUnauthorized()
    {
        var request = Request("""{"type":"door.manual"}""", ("x-token", "wrong-token"));

        var result = await Sut().ProcessAsync(request);

        Assert.Equal(InboundOutcome.Unauthorized, result.Outcome);
        await _eventService.DidNotReceive().PublishToEndpointAsync(
            Arg.Any<WebhookEndpoint>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task XTokenOnly_NoTypeAndNoHeader_IsInvalid()
    {
        // Matches the endpoint, but normalization can't find an event type → Invalid, nothing queued.
        var request = Request("""{"companyId":500646,"doorIds":["d1"]}""", ("x-token", Token));

        var result = await Sut().ProcessAsync(request);

        Assert.Equal(InboundOutcome.Invalid, result.Outcome);
        await _eventService.DidNotReceive().PublishToEndpointAsync(
            Arg.Any<WebhookEndpoint>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
