using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Tests;

public class TokenInboundAuthenticatorTests
{
    private readonly TokenInboundAuthenticator _sut = new(new MVPAPI.WebHook.Application.Services.TokenDecoder());

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

    // --- Identity matching: same credential, cosmetic BaseUrl/ApplicationName differences ---

    private static string Token(string baseUrl, string appName, string secret = "sec") =>
        MVPAPI.WebHook.Application.Common.TokenDecoder.Encode(baseUrl, "apikey-1", "cid-1", secret, appName, "500646");

    [Fact]
    public void Authenticate_SameIdentityDifferentBaseUrlAndAppName_Succeeds()
    {
        // Provisioned token vs an externally-issued one: identical ApiKey/ClientId/ClientSecret/CompanyId,
        // different host and app-name casing — should still match.
        var endpoint = Endpoint(token: Token("https://localhost:7200", "API_MVP_INTEGRATION_ZAPIER"));
        var presented = Token("https://api.mvpaccess.online", "api-mvp-integration-zapier");

        var result = _sut.Authenticate(Request(("x-token", presented)), endpoint);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Authenticate_SameIdentityDifferentClientSecret_Fails()
    {
        // The ClientSecret is part of the identity and is still verified.
        var endpoint = Endpoint(token: Token("https://localhost:7200", "app", secret: "right-secret"));
        var presented = Token("https://api.mvpaccess.online", "app", secret: "wrong-secret");

        var result = _sut.Authenticate(Request(("x-token", presented)), endpoint);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token.", result.Error);
    }
}
