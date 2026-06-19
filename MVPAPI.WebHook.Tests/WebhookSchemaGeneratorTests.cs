using MVPAPI.WebHook.Application.Common;

namespace MVPAPI.WebHook.Tests;

public class WebhookSchemaGeneratorTests
{
    [Theory]
    [InlineData("event.create", "Access Event Payload")]
    [InlineData("event.acknowledge", "Input Event Payload")]
    [InlineData("event.operatorresponse", "Relay Event Payload")]
    public void Generate_KnownTriggerType_ReturnsMatchingSchema(string triggerType, string expectedPayloadName)
    {
        var json = $"{{\"triggerType\":\"{triggerType}\",\"companyId\":42}}";

        var result = WebhookSchemaGenerator.Generate(json);

        Assert.True(result.IsSuccess);
        Assert.Equal(triggerType, result.Value!.TriggerType);
        Assert.Equal(expectedPayloadName, result.Value!.PayloadName);
        Assert.NotEmpty(result.Value!.Fields);
    }

    [Fact]
    public void Generate_CompanyId_FlowsIntoSchemaExample()
    {
        var result = WebhookSchemaGenerator.Generate("{\"triggerType\":\"event.create\",\"companyId\":99}");

        Assert.True(result.IsSuccess);
        var companyField = result.Value!.Fields.Single(f => f.Name == "companyId");
        Assert.Equal(99, companyField.Example);
    }

    [Fact]
    public void Generate_InvalidJson_Fails()
    {
        var result = WebhookSchemaGenerator.Generate("{ not json ");

        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid TriggerConfigJson.", result.Error);
    }

    [Fact]
    public void Generate_MissingTriggerType_Fails()
    {
        var result = WebhookSchemaGenerator.Generate("{\"companyId\":1}");

        Assert.False(result.IsSuccess);
        Assert.Equal("triggerType is required.", result.Error);
    }

    [Fact]
    public void Generate_UnsupportedTriggerType_Fails()
    {
        var result = WebhookSchemaGenerator.Generate("{\"triggerType\":\"event.unknown\"}");

        Assert.False(result.IsSuccess);
        Assert.Equal("Unsupported triggerType: event.unknown", result.Error);
    }
}
