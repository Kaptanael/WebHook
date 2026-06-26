using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Connections;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebHookConnectionService
{
    Task<Result<CreateConnectionResponse>> Connect(string token, CancellationToken cancellationToken);   
}
