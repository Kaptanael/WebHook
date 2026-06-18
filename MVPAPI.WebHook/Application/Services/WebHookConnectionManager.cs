using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services;

public class WebHookConnectionManager(
    ITokenValidator tokenValidator,
    IAccountApiClient accountApiClient,
    IOptions<MVPApiOptions> mvpApiOptions,
    IWebHookConnectionRepository connectionRepository) : IWebHookConnectionManager
{
    public async Task<Result<WebHookConnection>> EnsureConnectionAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenValidator.DecodeToken(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<WebHookConnection>(decodeResult.Error!);

        var decoded = decodeResult.Value!;

        var tokenResponse = await accountApiClient.CallTokenApiAsync(
            url: mvpApiOptions.Value.TokenUrl,
            apiKey: decoded.ApiKey,
            grantType: "client_credentials",
            clientId: decoded.ClientId,
            clientSecret: decoded.ClientSecret,
            companyId: decoded.CompanyId,
            cancellationToken
        );

        if (tokenResponse is null)
            return Result.Failure<WebHookConnection>("Token response is null.");

        if (!tokenResponse.Success)
            return Result.Failure<WebHookConnection>($"Token request failed: {tokenResponse.Error}");

        var existing = await connectionRepository.GetByClientTokenAsync(token, cancellationToken);
        if (existing is not null)
        {            
            return Result.Success(existing);
        }

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
