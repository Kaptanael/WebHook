using MVPAPI.WebHook.Application.Interfaces.Inbound;
using MVPAPI.WebHook.Application.Services.Inbound;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Tests;

public class DefaultPayloadAdapterTests
{
    private readonly DefaultPayloadAdapter _sut = new();

    private static WebhookEndpoint Endpoint() =>
        new() { Endpoint = "https://acme.example/hook", CompanyId = 1 };

    private static InboundRequest Request(string body, params (string Key, string Value)[] headers) =>
        new(headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase), body);

    [Fact]
    public void Normalize_ReadsEventTypeFromBodyType()
    {
        var body = """{"type":"contact.created","timestamp":"2022-11-03T20:26:10.344522Z","data":{"id":"abc"}}""";

        var result = _sut.Normalize(Request(body), Endpoint());

        Assert.True(result.IsSuccess);
        Assert.Equal("contact.created", result.Value!.EventType);
        Assert.Equal(body, result.Value.Payload); // whole envelope delivered verbatim
    }

    [Fact]
    public void Normalize_BodyTypeWinsOverHeader()
    {
        var body = """{"type":"contact.created"}""";

        var result = _sut.Normalize(Request(body, (DefaultPayloadAdapter.EventTypeHeader, "door.manual")), Endpoint());

        Assert.True(result.IsSuccess);
        Assert.Equal("contact.created", result.Value!.EventType);
    }

    [Fact]
    public void Normalize_FallsBackToHeaderWhenNoBodyType()
    {
        var body = """{"companyId":500646,"doorIds":["x"]}""";

        var result = _sut.Normalize(Request(body, (DefaultPayloadAdapter.EventTypeHeader, "door.manual")), Endpoint());

        Assert.True(result.IsSuccess);
        Assert.Equal("door.manual", result.Value!.EventType);
    }

    [Fact]
    public void Normalize_FallsBackToHeaderWhenBodyNotJson()
    {
        var result = _sut.Normalize(Request("not json", (DefaultPayloadAdapter.EventTypeHeader, "door.manual")), Endpoint());

        Assert.True(result.IsSuccess);
        Assert.Equal("door.manual", result.Value!.EventType);
    }

    [Fact]
    public void Normalize_EmptyBody_Fails()
    {
        var result = _sut.Normalize(Request("  "), Endpoint());

        Assert.False(result.IsSuccess);
        Assert.Equal("Payload cannot be empty.", result.Error);
    }

    [Fact]
    public void Normalize_NoTypeAndNoHeader_Fails()
    {
        var result = _sut.Normalize(Request("""{"data":{"id":"abc"}}"""), Endpoint());

        Assert.False(result.IsSuccess);
        Assert.Contains("Event type not found", result.Error);
    }
}
