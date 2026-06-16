using System.Text.Json.Serialization;

namespace MVPAPI.WebHook.Application.DTOs.Tokens;

public record TokenDecoderResponse(
    string BaseUrl,
    string ApiKey,
    string ClientId,
    string ClientSecret,
    string ApplicationName,
    int CompanyId);

public sealed class TokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("expiresIn")]
    public DateTime ExpiresIn { get; set; }

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}