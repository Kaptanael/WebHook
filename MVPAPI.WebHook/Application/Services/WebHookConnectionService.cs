using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Connections;
using MVPAPI.WebHook.Application.Interfaces.Services;

namespace MVPAPI.WebHook.Application.Services;

public class WebHookConnectionService(
IWebhookManager webhookManager) : IWebHookConnectionService
{
    public async Task<Result<CreateConnectionResponse>> Connect(string token, CancellationToken cancellationToken = default)
    {
        var result = await webhookManager.EnsureConnectionAsync(token, cancellationToken);
        if (!result.IsSuccess)
            return Result.Failure<CreateConnectionResponse>(result.Error!);

        return Result.Success(new CreateConnectionResponse(true, "Connection established successfully."));
    }
}
