using MVPAPI.WebHook.Application.Services;
using System.Security.Cryptography;
using System.Text;

namespace MVPAPI.WebHook.Tests;

public class StandardWebhookSignerTests
{
    private readonly StandardWebhookSigner _sut = new();

    private static readonly DateTimeOffset Timestamp = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    [Fact]
    public void Sign_EchoesMessageIdAndUnixTimestamp()
    {
        var headers = _sut.Sign("msg-1", Timestamp, "{\"a\":1}", "whsecret");

        Assert.Equal("msg-1", headers.Id);
        Assert.Equal("1700000000", headers.Timestamp);
    }

    [Fact]
    public void Sign_ProducesVersionedBase64SignatureOverIdTimestampPayload()
    {
        const string secret = "whsecret";
        const string payload = "{\"a\":1}";
        var expected = "v1," + ComputeHmacBase64(secret, $"msg-1.1700000000.{payload}");

        var headers = _sut.Sign("msg-1", Timestamp, payload, secret);

        Assert.Equal(expected, headers.Signature);
    }

    [Fact]
    public void Sign_DifferentPayload_ProducesDifferentSignature()
    {
        var a = _sut.Sign("msg-1", Timestamp, "{\"a\":1}", "whsecret");
        var b = _sut.Sign("msg-1", Timestamp, "{\"a\":2}", "whsecret");

        Assert.NotEqual(a.Signature, b.Signature);
    }

    [Fact]
    public void Sign_DifferentSecret_ProducesDifferentSignature()
    {
        var a = _sut.Sign("msg-1", Timestamp, "{\"a\":1}", "secret-one");
        var b = _sut.Sign("msg-1", Timestamp, "{\"a\":1}", "secret-two");

        Assert.NotEqual(a.Signature, b.Signature);
    }

    [Fact]
    public void Sign_WhsecPrefixedSecret_KeysWithBase64DecodedBytes()
    {
        var keyBytes = Encoding.UTF8.GetBytes("the-real-key");
        var secret = "whsec_" + Convert.ToBase64String(keyBytes);
        const string payload = "{\"a\":1}";
        var expected = "v1," + ComputeHmacBase64(keyBytes, $"msg-1.1700000000.{payload}");

        var headers = _sut.Sign("msg-1", Timestamp, payload, secret);

        Assert.Equal(expected, headers.Signature);
    }

    [Fact]
    public void Sign_WhsecPrefixWithInvalidBase64_FallsBackToRawBytes()
    {
        const string secret = "whsec_not valid base64!";
        const string payload = "{\"a\":1}";
        var expected = "v1," + ComputeHmacBase64(Encoding.UTF8.GetBytes(secret), $"msg-1.1700000000.{payload}");

        var headers = _sut.Sign("msg-1", Timestamp, payload, secret);

        Assert.Equal(expected, headers.Signature);
    }

    private static string ComputeHmacBase64(string secret, string message)
        => ComputeHmacBase64(Encoding.UTF8.GetBytes(secret), message);

    private static string ComputeHmacBase64(byte[] key, string message)
    {
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
    }
}
