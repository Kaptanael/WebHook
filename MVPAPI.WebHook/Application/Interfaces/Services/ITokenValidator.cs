using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Tokens;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface ITokenValidator
{
    Result<TokenDecoderResponse> DecodeToken(string token);
}
