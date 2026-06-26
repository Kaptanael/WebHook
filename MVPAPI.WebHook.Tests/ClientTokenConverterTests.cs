using MVPAPI.WebHook.Application.Services;
using System.Text;

namespace MVPAPI.WebHook.Tests;

public class TokenDecoderTests
{
    private readonly TokenDecoder _sut = new();

    // The token is a base64 (url-safe tolerated) encoding of newline-delimited fields:
    // url \n apiKey \n clientId \n clientSecret \n applicationName \n companyId
    private static string Encode(string raw) => Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));

    [Fact]
    public void Decode_ValidToken_ReturnsAllFields()
    {
        var token = Encode("S|api.example.com\nthe-api-key\nclient-1\nsecret-1\nMyApp\n42");

        var result = _sut.Decode(token);

        Assert.True(result.IsSuccess);
        var value = result.Value!;
        Assert.Equal("https://api.example.com", value.BaseUrl);
        Assert.Equal("the-api-key", value.ApiKey);
        Assert.Equal("client-1", value.ClientId);
        Assert.Equal("secret-1", value.ClientSecret);
        Assert.Equal("MyApp", value.ApplicationName);
        Assert.Equal(42, value.CompanyId);
    }

    [Fact]
    public void Decode_HttpPrefix_ExpandsToHttpScheme()
    {
        var token = Encode("H|api.local\nkey\nclient\nsecret\nApp\n7");

        var result = _sut.Decode(token);

        Assert.True(result.IsSuccess);
        Assert.Equal("http://api.local", result.Value!.BaseUrl);
    }

    [Fact]
    public void Decode_EmptyToken_FailsWithRequiredMessage()
    {
        var result = _sut.Decode("");

        Assert.False(result.IsSuccess);
        Assert.Equal("Token is required.", result.Error);
    }

    [Fact]
    public void Decode_NonBase64_FailsWithInvalidMessage()
    {
        var result = _sut.Decode("@@@@");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token.", result.Error);
    }

    [Fact]
    public void Decode_TooFewParts_FailsWithInvalidMessage()
    {
        var token = Encode("S|api\nkey\nclient"); // only 3 fields

        var result = _sut.Decode(token);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token.", result.Error);
    }

    [Fact]
    public void Decode_NonNumericCompanyId_FailsWithInvalidMessage()
    {
        var token = Encode("S|api\nkey\nclient\nsecret\nApp\nNOT_AN_INT");

        var result = _sut.Decode(token);

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid token.", result.Error);
    }
}
