using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Tests;

public class ApiKeyInboundAuthenticatorTests
{
    private readonly ApiKeyInboundAuthenticator _sut = new();

    private static WebhookEndpoint Endpoint(string token = "secret-key") =>
        new() { EndPointToken = token, Endpoint = "https://acme.example/hook", CompanyId = 1 };

    private static InboundRequest Request(params (string Key, string Value)[] headers) =>
        new(headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase), "{}");

    [Fact]
    public void Authenticate_MatchingKey_Succeeds()
    {
        var result = _sut.Authenticate(Request(("X-Api-Key", "secret-key")), Endpoint());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Authenticate_WrongKey_Fails()
    {
        var result = _sut.Authenticate(Request(("X-Api-Key", "nope")), Endpoint());

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid API key.", result.Error);
    }

    [Fact]
    public void Authenticate_MissingHeader_Fails()
    {
        var result = _sut.Authenticate(Request(), Endpoint());

        Assert.False(result.IsSuccess);
        Assert.Equal("Missing x-api-key header.", result.Error);
    }

    [Fact]
    public void Authenticate_EndpointWithoutToken_Fails()
    {
        var result = _sut.Authenticate(Request(("X-Api-Key", "x")), Endpoint(token: ""));

        Assert.False(result.IsSuccess);
        Assert.Equal("Endpoint has no token configured.", result.Error);
    }
}
