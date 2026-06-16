using MVPAPI.WebHook.Application.DTOs.Tokens;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IAccountApiClient
{
    Task<TokenResponse> CallTokenApiAsync(
        string url,        
        string apiKey,        
        string grantType,
        string clientId,
        string clientSecret,
        int companyId,
        CancellationToken ct = default);    
}
