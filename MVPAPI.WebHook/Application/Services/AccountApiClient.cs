namespace MVPAPI.WebHook.Application.Services;

using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Tokens;
using MVPAPI.WebHook.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class AccountApiClient(
    HttpClient httpClient, 
    IOptions<MVPApiOptions> mvpApiOptions) : IAccountApiClient
{
    public async Task<Result<TokenResponse>> GetTokenAsync(    
    string apiKey,
    string grantType,
    string clientId,
    string clientSecret,
    int companyId,
    CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, mvpApiOptions.Value.TokenUrl);

            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("api-key", apiKey);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            var basicAuthorization = new AuthenticationHeaderValue("Basic", credentials).ToString();

            if (!string.IsNullOrWhiteSpace(basicAuthorization))
                request.Headers.TryAddWithoutValidation("Authorization", basicAuthorization);

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
                return Result<TokenResponse>.Failure($"Token request failed: {response.StatusCode} - {content}");

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse is null)
                return Result<TokenResponse>.Failure("Failed to deserialize token response.");

            if (!tokenResponse.Success)
                return Result<TokenResponse>.Failure($"Token error: {tokenResponse.Error}");

            return Result<TokenResponse>.Success(tokenResponse);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<TokenResponse>.Failure($"Token request errored: {ex.Message}");
        }
    }

    public async Task<Result<TokenResponse>> GetRefreshTokenAsync(    
    string apiKey,
    string clientId,
    string clientSecret,
    string refreshToken,
    CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, mvpApiOptions.Value.TokenUrl);

            if (!string.IsNullOrWhiteSpace(apiKey))
                request.Headers.Add("api-key", apiKey);

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            var basicAuthorization = new AuthenticationHeaderValue("Basic", credentials).ToString();

            if (!string.IsNullOrWhiteSpace(basicAuthorization))
                request.Headers.TryAddWithoutValidation("Authorization", basicAuthorization);

            request.Content = new StringContent(
                JsonSerializer.Serialize(new RefreshTokenRequest(refreshToken)),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(request, ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return Result<TokenResponse>.Failure(
                    $"Refresh token request failed: {response.StatusCode} - {content}");

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (tokenResponse is null)
                return Result<TokenResponse>.Failure("Failed to deserialize refresh token response.");

            if (!tokenResponse.Success)
                return Result<TokenResponse>.Failure($"Refresh token error: {tokenResponse.Error}");

            return Result<TokenResponse>.Success(tokenResponse);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Result<TokenResponse>.Failure($"Refresh token request errored: {ex.Message}");
        }
    }        
}