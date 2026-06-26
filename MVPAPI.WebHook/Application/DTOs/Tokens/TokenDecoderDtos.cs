using System.Text.Json.Serialization;

namespace MVPAPI.WebHook.Application.DTOs.Tokens;

public record RefreshTokenRequest(
    [property: JsonPropertyName("refreshToken")] string RefreshToken);

public record TokenResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("refreshToken")] string RefreshToken,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("expiresIn")] DateTime ExpiresIn,
    [property: JsonPropertyName("error")] string Error);
