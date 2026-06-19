using MVPAPI.WebHook.Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Tests;

public class WebhookSignatureVerifierTests
{
    private readonly WebhookSignatureVerifier _sut = new();

    private static string CurrentTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    [Fact]
    public void ValidateHeaders_AllPresentAndFresh_ReturnsSuccess()
    {
        var result = _sut.ValidateHeaders(CurrentTimestamp(), "sig", "token");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ValidateHeaders_MissingTimestamp_Fails()
    {
        var result = _sut.ValidateHeaders("", "sig", "token");

        Assert.False(result.IsSuccess);
        Assert.Equal("Missing X-Timestamp header.", result.Error);
    }

    [Fact]
    public void ValidateHeaders_NonNumericTimestamp_Fails()
    {
        var result = _sut.ValidateHeaders("not-a-number", "sig", "token");

        Assert.False(result.IsSuccess);
        Assert.Equal("Request timestamp is invalid or expired.", result.Error);
    }

    [Fact]
    public void ValidateHeaders_ExpiredTimestamp_Fails()
    {
        var stale = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 301).ToString();

        var result = _sut.ValidateHeaders(stale, "sig", "token");

        Assert.False(result.IsSuccess);
        Assert.Equal("Request timestamp is invalid or expired.", result.Error);
    }

    [Fact]
    public void ValidateHeaders_MissingSignature_Fails()
    {
        var result = _sut.ValidateHeaders(CurrentTimestamp(), "", "token");

        Assert.False(result.IsSuccess);
        Assert.Equal("Missing X-Signature header.", result.Error);
    }

    [Fact]
    public void ValidateHeaders_MissingToken_Fails()
    {
        var result = _sut.ValidateHeaders(CurrentTimestamp(), "sig", "");

        Assert.False(result.IsSuccess);
        Assert.Equal("Missing X-Endpoint-Token header.", result.Error);
    }

    [Fact]
    public void VerifySignature_CorrectSignature_ReturnsSuccess()
    {
        const string apiKey = "secret-key";
        var timestamp = CurrentTimestamp();
        const string payload = "{\"a\":1}";
        var signature = ComputeHmacHex(apiKey, $"{timestamp}.{payload}");

        var result = _sut.VerifySignature(apiKey, timestamp, payload, signature);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void VerifySignature_WrongSignature_Fails()
    {
        var result = _sut.VerifySignature("secret-key", CurrentTimestamp(), "{}", "deadbeef");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid signature.", result.Error);
    }

    [Fact]
    public void VerifySignature_TamperedPayload_Fails()
    {
        const string apiKey = "secret-key";
        var timestamp = CurrentTimestamp();
        var signature = ComputeHmacHex(apiKey, $"{timestamp}.{{\"a\":1}}");

        // Same signature, but the payload no longer matches what was signed.
        var result = _sut.VerifySignature(apiKey, timestamp, "{\"a\":2}", signature);

        Assert.False(result.IsSuccess);
    }

    private static string ComputeHmacHex(string secret, string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(message))).ToLowerInvariant();
    }
}
