using Microsoft.Extensions.Logging.Abstractions;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Services;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;
using NSubstitute;
using System.Text;

namespace MVPAPI.WebHook.Tests;

public class ApiKeyStandardWebhookResolverTests
{
    private const string RawKey = "raw-api-key-123";
    // Salt is base64 (as stored in PortalDB); the HMAC key is its decoded bytes.
    private static readonly string Salt = Convert.ToBase64String(Encoding.UTF8.GetBytes("per-key-salt-secret"));
    private const string Body = "{\"order\":1}";
    private const int CompanyId = 42;

    private readonly IApiKeyRepository _repo = Substitute.For<IApiKeyRepository>();
    private readonly StandardWebhookSigner _signer = new();
    private readonly ApiKeyStandardWebhookResolver _sut;

    public ApiKeyStandardWebhookResolverTests()
    {
        _sut = new ApiKeyStandardWebhookResolver(_repo, _signer, NullLogger<ApiKeyStandardWebhookResolver>.Instance);
    }

    private static ApiKey Key(string? salt = null, string status = "Active") =>
        new() { Id = Guid.NewGuid(), RawApiKey = RawKey, Salt = salt ?? Salt, CompanyId = CompanyId, Status = status };

    private InboundRequest SignedRequest(string? salt = null, string id = "msg-1", string body = Body,
        DateTimeOffset? timestamp = null, string apiKey = RawKey)
    {
        var ts = timestamp ?? DateTimeOffset.UtcNow;
        // Sign exactly as the resolver verifies: salt prefixed so its bytes are base64-decoded.
        var sig = _signer.Sign(id, ts, body, WebhookSigningSecret.Prefix + (salt ?? Salt));
        return Request(body,
            ("X-Api-Key", apiKey),
            ("webhook-id", id),
            ("webhook-timestamp", ts.ToUnixTimeSeconds().ToString()),
            ("webhook-signature", sig.Signature));
    }

    private static InboundRequest Request(string body, params (string Key, string Value)[] headers) =>
        new(headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase), body);

    [Fact]
    public async Task NoApiKeyHeader_NotPresented()
    {
        var result = await _sut.ResolveAsync(Request(Body, ("webhook-id", "x")));

        Assert.Equal(ApiKeyAuthOutcome.NotPresented, result.Outcome);
    }

    [Fact]
    public async Task ValidKeyAndSignature_Authenticated()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key());

        var result = await _sut.ResolveAsync(SignedRequest());

        Assert.Equal(ApiKeyAuthOutcome.Authenticated, result.Outcome);
        Assert.Equal(CompanyId, result.CompanyId);
    }

    [Fact]
    public async Task UnknownKey_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns((ApiKey?)null);

        var result = await _sut.ResolveAsync(SignedRequest());

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("Invalid API key.", result.Error);
    }

    [Fact]
    public async Task InactiveKey_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key(status: "Revoked"));

        var result = await _sut.ResolveAsync(SignedRequest());

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("Invalid API key.", result.Error);
    }

    [Fact]
    public async Task SignatureSignedWithWrongSalt_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key());

        // Sender signed with a different (valid base64) salt than the stored key.
        var wrongSalt = Convert.ToBase64String(Encoding.UTF8.GetBytes("different-salt"));
        var result = await _sut.ResolveAsync(SignedRequest(salt: wrongSalt));

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("Invalid signature.", result.Error);
    }

    [Fact]
    public async Task TamperedBody_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key());

        var ts = DateTimeOffset.UtcNow;
        var sig = _signer.Sign("msg-1", ts, Body, WebhookSigningSecret.Prefix + Salt);
        var tampered = Request("{\"order\":999}",
            ("X-Api-Key", RawKey),
            ("webhook-id", "msg-1"),
            ("webhook-timestamp", ts.ToUnixTimeSeconds().ToString()),
            ("webhook-signature", sig.Signature));

        var result = await _sut.ResolveAsync(tampered);

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("Invalid signature.", result.Error);
    }

    [Fact]
    public async Task ExpiredTimestamp_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key());

        var old = DateTimeOffset.UtcNow.AddMinutes(-10);
        var result = await _sut.ResolveAsync(SignedRequest(timestamp: old));

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("Request timestamp is invalid or expired.", result.Error);
    }

    [Fact]
    public async Task MissingTriplet_Rejected()
    {
        var result = await _sut.ResolveAsync(Request(Body, ("X-Api-Key", RawKey)));

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("Missing webhook-id header.", result.Error);
    }

    [Fact]
    public async Task KeyWithoutSalt_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key(salt: ""));

        var result = await _sut.ResolveAsync(SignedRequest());

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("API key has no signing secret configured.", result.Error);
    }

    [Fact]
    public async Task NonBase64Salt_Rejected()
    {
        _repo.GetByRawApiKeyAsync(RawKey, Arg.Any<CancellationToken>()).Returns(Key(salt: "not valid base64!"));

        var result = await _sut.ResolveAsync(SignedRequest());

        Assert.Equal(ApiKeyAuthOutcome.Rejected, result.Outcome);
        Assert.Equal("API key salt is not valid base64.", result.Error);
    }
}
