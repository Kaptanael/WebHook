namespace MVPAPI.WebHook.Application.Services;

using MVPAPI.WebHook.Application.Common.Exceptions;
using MVPAPI.WebHook.Application.DTOs.Tokens;
using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class AccountApiClient(HttpClient httpClient) : IAccountApiClient
{
    public async Task<TokenResponse> CallTokenApiAsync(
    string url,    
    string apiKey,    
    string grantType,
    string clientId,
    string clientSecret,
    int companyId,
    CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);        

        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Add("api-key", apiKey);

        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        var authorization = new AuthenticationHeaderValue("Basic", credentials).ToString();

        if (!string.IsNullOrWhiteSpace(authorization))
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = grantType,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["company_id"] = companyId.ToString()
        });

        using var response = await httpClient.SendAsync(request, ct);

        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new BadRequestException($"Token request failed: {response.StatusCode} - {content}");

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tokenResponse is null)
            throw new BadRequestException("Failed to deserialize token response.");

        if (!tokenResponse.Success)
            throw new BadRequestException($"Token error: {tokenResponse.Error}");

        return tokenResponse;
    }

    private static string GenerateBasicAuth(string clientId, string clientSecret)
    {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        return $"Basic {encoded}";
    }
}