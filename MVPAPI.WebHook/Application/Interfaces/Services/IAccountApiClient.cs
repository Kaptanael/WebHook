using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Tokens;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IAccountApiClient
{
    Task<Result<TokenResponse>> GetTokenAsync(        
        string apiKey,
        string grantType,
        string clientId,
        string clientSecret,
        int companyId,
        CancellationToken ct = default);

    Task<Result<TokenResponse>> GetRefreshTokenAsync(        
        string apiKey,
        string clientId,
        string clientSecret,
        string refreshToken,
        CancellationToken ct = default);
}
