using MVPAPI.WebHook.Application.Common;

namespace MVPAPI.WebHook.Tests;

public class WebhookSigningSecretTests
{
    [Fact]
    public void Generate_HasWhsecPrefix()
    {
        Assert.StartsWith("whsec_", WebhookSigningSecret.Generate());
    }

    [Fact]
    public void Generate_RemainderIsBase64Of24Bytes()
    {
        var secret = WebhookSigningSecret.Generate();
        var encoded = secret["whsec_".Length..];

        var bytes = Convert.FromBase64String(encoded);

        Assert.Equal(24, bytes.Length);
    }

    [Fact]
    public void Generate_ProducesDistinctSecrets()
    {
        Assert.NotEqual(WebhookSigningSecret.Generate(), WebhookSigningSecret.Generate());
    }
}
