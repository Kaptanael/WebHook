using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services;

public class WebHookConnectionManager(
    ITokenDecoder tokenDecoder,
    IAccountApiClient accountApiClient,    
    IWebHookConnectionRepository connectionRepository) : IWebHookConnectionManager
{
    public async Task<Result<WebHookConnection>> EnsureConnectionAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenDecoder.Decode(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<WebHookConnection>(decodeResult.Error!);

        var decoded = decodeResult.Value!;

        var tokenResult = await accountApiClient.GetTokenAsync(            
            apiKey: decoded.ApiKey,
            grantType: "client_credentials",
            clientId: decoded.ClientId,
            clientSecret: decoded.ClientSecret,
            companyId: decoded.CompanyId,
            cancellationToken);

        if (!tokenResult.IsSuccess)
            return Result.Failure<WebHookConnection>(tokenResult.Error!);

        var tokenResponse = tokenResult.Value!;

        var existing = await connectionRepository.GetByClientTokenAsync(token, cancellationToken);
        if (existing is not null)
            return Result.Success(existing);

        var connection = new WebHookConnection
        {
            CompanyId = decoded.CompanyId,
            ApplicationName = decoded.ApplicationName,
            IsActive = true,
            ClientToken = token,
            MVPApiToken = tokenResponse.Token,
            MVPApiRefreshToken = tokenResponse.RefreshToken,
            MVPApiExpiresIn = tokenResponse.ExpiresIn,
        };

        await connectionRepository.AddAsync(connection, cancellationToken);
        return Result.Success(connection);
    }
}


