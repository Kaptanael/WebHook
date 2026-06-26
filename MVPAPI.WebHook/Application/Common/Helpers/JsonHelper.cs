using System.Text.Json;

namespace MVPAPI.WebHook.Application.Common.Helpers;

public class JsonHelper
{
    public static string? GetType(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                doc.RootElement.TryGetProperty("type", out var typeElement) &&
                typeElement.ValueKind == JsonValueKind.String)
                return typeElement.GetString();
        }
        catch (JsonException)
        {
            // Body is not JSON.
        }

        return null;
    }
}
