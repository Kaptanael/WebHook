using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Tests;

public class TokenInboundAuthenticatorTests
{
    private readonly TokenInboundAuthenticator _sut = new();

    private static WebhookEndpoint Endpoint(string token = "s3cret") =>
        new() { EndPointToken = token, Endpoint = "https://acme.example/hook", CompanyId = 1 };

    private static InboundRequest Request(params (string Key, string Value)[] headers) =>
        new(headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase), "{}");

    [Fact]
    public void Authenticate_MatchingRawToken_Succeeds()
    {
        var result = _sut.Authenticate(Request(("x-token","s3cret")), Endpoint());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Authenticate_TrimsSurroundingWhitespace()
    {
        var result = _sut.Authenticate(Request(("x-token","  s3cret  ")), Endpoint());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Authenticate_WrongToken_Fails()
    {
        var result = _sut.Authenticate(Request(("x-token","nope")), Endpoint());

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token.", result.Error);
    }

    [Fact]
    public void Authenticate_MissingHeader_Fails()
    {
        var result = _sut.Authenticate(Request(), Endpoint());

        Assert.False(result.IsSuccess);
        Assert.Equal("Missing x-token header.", result.Error);
    }

    [Fact]
    public void Authenticate_EndpointWithoutToken_Fails()
    {
        var result = _sut.Authenticate(Request(("x-token","x")), Endpoint(token: ""));

        Assert.False(result.IsSuccess);
        Assert.Equal("Endpoint has no token configured.", result.Error);
    }
}
