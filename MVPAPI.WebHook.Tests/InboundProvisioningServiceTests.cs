using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;
using NSubstitute;

namespace MVPAPI.WebHook.Tests;

public class InboundProvisioningServiceTests
{
    private const int CompanyId = 100;
    private const string RawApiKey = "raw-api-key-123";
    private const string EventType = "door.manual";
    private const string DeliveryUrl = "http://internal/api/ManualDoor/Execute";
    private const string BaseUrl = "https://mvp.example.com";

    private readonly IClientCredentialRepository _credentials = Substitute.For<IClientCredentialRepository>();
    private readonly IWebHookConnectionManager _connectionManager = Substitute.For<IWebHookConnectionManager>();
    private readonly IWebhookEndpointRepository _endpoints = Substitute.For<IWebhookEndpointRepository>();

    private InboundProvisioningService Sut(bool withRoute = true) => new(
        _credentials,
        _connectionManager,
        _endpoints,
        Options.Create(new WebhookRouteOptions { Routes = withRoute ? new() { [EventType] = DeliveryUrl } : new() }),
        Options.Create(new MVPApiOptions { BaseUrl = BaseUrl }),
        NullLogger<InboundProvisioningService>.Instance);

    private void GivenCredential() =>
        _credentials.GetActiveByCompanyIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns(new ClientCredential { CompanyId = CompanyId, ClientId = "cid", Secret = "sec", IsActive = true });

    private void GivenConnectionSucceeds() =>
        _connectionManager.EnsureConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new WebHookConnection { CompanyId = CompanyId, IsActive = true }));

    [Fact]
    public async Task HappyPath_CreatesEndpoint_WithTokenThatDecodesToTheKey()
    {
        GivenCredential();
        GivenConnectionSucceeds();
        _endpoints.GetActiveByCompanyAndEventTypeAsync(CompanyId, EventType, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<WebhookEndpoint>());
        _endpoints.AddAsync(Arg.Any<WebhookEndpoint>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        var endpoint = await Sut().EnsureProvisionedAsync(CompanyId, RawApiKey, EventType);

        Assert.NotNull(endpoint);
        Assert.Equal(DeliveryUrl, endpoint!.Endpoint);
        Assert.Equal(CompanyId, endpoint.CompanyId);

        // The minted EndPointToken must decode back to the originating key + the looked-up credential.
        var decoded = TokenDecoder.Decode(endpoint.EndPointToken);
        Assert.NotNull(decoded);
        Assert.Equal(RawApiKey, decoded!.ApiKey);
        Assert.Equal("cid", decoded.ClientId);
        Assert.Equal("sec", decoded.ClientSecret);
        Assert.Equal(CompanyId, decoded.CompanyId);
        Assert.Equal(BaseUrl, decoded.BaseUrl);

        await _connectionManager.Received(1).EnsureConnectionAsync(endpoint.EndPointToken, Arg.Any<CancellationToken>());
        await _endpoints.Received(1).AddAsync(Arg.Any<WebhookEndpoint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReusesExistingEndpoint_WhenOneAlreadyExistsForEventType()
    {
        GivenCredential();
        GivenConnectionSucceeds();
        var existing = new WebhookEndpoint { Id = Guid.NewGuid(), CompanyId = CompanyId, Endpoint = DeliveryUrl, IsActive = true };
        _endpoints.GetActiveByCompanyAndEventTypeAsync(CompanyId, EventType, Arg.Any<CancellationToken>())
            .Returns(new[] { existing });

        var endpoint = await Sut().EnsureProvisionedAsync(CompanyId, RawApiKey, EventType);

        Assert.Same(existing, endpoint);
        await _endpoints.DidNotReceive().AddAsync(Arg.Any<WebhookEndpoint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoRouteForEventType_ReturnsNull_AndNeverTouchesCredentials()
    {
        var endpoint = await Sut(withRoute: false).EnsureProvisionedAsync(CompanyId, RawApiKey, EventType);

        Assert.Null(endpoint);
        await _credentials.DidNotReceive().GetActiveByCompanyIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NoActiveCredential_ReturnsNull_AndNeverProvisionsConnection()
    {
        _credentials.GetActiveByCompanyIdAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((ClientCredential?)null);

        var endpoint = await Sut().EnsureProvisionedAsync(CompanyId, RawApiKey, EventType);

        Assert.Null(endpoint);
        await _connectionManager.DidNotReceive().EnsureConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConnectionFailure_ReturnsNull_AndNeverCreatesEndpoint()
    {
        GivenCredential();
        _connectionManager.EnsureConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<WebHookConnection>("token endpoint down"));

        var endpoint = await Sut().EnsureProvisionedAsync(CompanyId, RawApiKey, EventType);

        Assert.Null(endpoint);
        await _endpoints.DidNotReceive().AddAsync(Arg.Any<WebhookEndpoint>(), Arg.Any<CancellationToken>());
    }
}
