using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Tokens;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services;

public class TokenValidator : ITokenValidator
{
    public Result<TokenDecoderResponse> DecodeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Result<TokenDecoderResponse>.Failure("Token is required.");

        var decodedToken = TokenDecoder.Decode(token);
        if (decodedToken is null)
            return Result<TokenDecoderResponse>.Failure("Invalid token.");

        return Result<TokenDecoderResponse>.Success(decodedToken);
    }
}
