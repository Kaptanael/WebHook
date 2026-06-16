using Microsoft.Extensions.Options;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Connections;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services;

public class WebHookConnectionService(
    IAccountApiClient accountApiClient,
    IOptions<MVPApiOptions> mvpApiOptions,
    ITokenValidator tokenValidator,
    IWebHookConnectionRepository webHookConnectionRepository) : IWebHookConnectionService
{
    public async Task<Result<CreateConnectionResponse>> Connect(string token, CancellationToken cancellationToken = default)
    {
        var decodeResult = tokenValidator.DecodeToken(token);
        if (!decodeResult.IsSuccess)
            return Result.Failure<CreateConnectionResponse>(decodeResult.Error!);

        var decodedToken = decodeResult.Value!;

        var tokenResponse = await accountApiClient.CallTokenApiAsync(
            url: mvpApiOptions.Value.TokenUrl,
            apiKey: decodedToken.ApiKey,
            grantType: "client_credentials",
            clientId: decodedToken.ClientId,
            clientSecret: decodedToken.ClientSecret,
            companyId: decodedToken.CompanyId,
            cancellationToken
        );

        if (tokenResponse is null)
            return Result.Failure<CreateConnectionResponse>("Token response is null.");

        if (!tokenResponse.Success)
            return Result.Failure<CreateConnectionResponse>($"Token request failed: {tokenResponse.Error}");

        var existingConnection = await webHookConnectionRepository.GetByClientTokenAsync(token, cancellationToken);
        if (existingConnection is not null)
        {
            existingConnection.ClientToken = token;            
            existingConnection.IsActive = true;
            existingConnection.MVPApiToken = tokenResponse.Token;
            existingConnection.MVPApiRefreshToken = tokenResponse.RefreshToken;
            existingConnection.MVPApiExpiresIn = tokenResponse.ExpiresIn;
            await webHookConnectionRepository.UpdateAsync(existingConnection, cancellationToken);
            return Result.Success(new CreateConnectionResponse(true, "Connection updated successfully."));
        }

        var webHookConnection = new WebHookConnection
        {
            CompanyId = decodedToken.CompanyId,
            ApplicationName = decodedToken.ApplicationName,
            IsActive = true,
            ClientToken = token,            
            MVPApiToken = tokenResponse.Token,
            MVPApiRefreshToken = tokenResponse.RefreshToken,
            MVPApiExpiresIn = tokenResponse.ExpiresIn,
        };

        await webHookConnectionRepository.AddAsync(webHookConnection, cancellationToken);
        return Result.Success(new CreateConnectionResponse(true, "Connection created successfully."));
    }    
}
