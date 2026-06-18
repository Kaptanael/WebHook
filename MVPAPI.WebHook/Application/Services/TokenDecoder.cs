using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Tokens;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services;

public class TokenDecoder : ITokenDecoder
{
    public Result<TokenDecoderResponse> Decode(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<TokenDecoderResponse>.Failure("Token is required.");

        var decodedToken = Common.TokenDecoder.Decode(token);
        if (decodedToken is null)
            return Result<TokenDecoderResponse>.Failure("Invalid token.");

        return Result<TokenDecoderResponse>.Success(decodedToken);
    }
}
