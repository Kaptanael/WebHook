using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebHookConnectionManager
{
    Task<Result<WebHookConnection>> EnsureConnectionAsync(
        string token,
        CancellationToken cancellationToken = default);
}
