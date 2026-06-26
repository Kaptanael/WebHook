using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Inbound;
using System.Text.Json;

namespace MVPAPI.WebHook.Application.Services.Inbound;

public class PayloadNormalizer
{
    public Result<NormalizedInboundEvent> Normalize(InboundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawBody))
            return Result.Failure<NormalizedInboundEvent>("Payload cannot be empty.");

        var eventType = ReadEventTypeFromBody(request.RawBody);
        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<NormalizedInboundEvent>(
                $"Event type not found: include a \"type\" field in the body.");

        return Result.Success(new NormalizedInboundEvent(eventType.Trim(), request.RawBody));
    }

    private static string? ReadEventTypeFromBody(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("type", out var typeElement) &&
                typeElement.ValueKind == JsonValueKind.String)
                return typeElement.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
        return null;
    }
}
