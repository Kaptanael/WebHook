using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Connections;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IWebHookConnectionService
{
    Task<Result<CreateConnectionResponse>> Connect(string token, CancellationToken cancellationToken);
    //Task<ConnectionResponse> CreateAsync(CreateConnectionRequest request, CancellationToken cancellationToken = default);
    //Task<ConnectionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    //Task<ConnectionResponse?> GetByCompanyIdAsync(int companyId, CancellationToken cancellationToken = default);
    //Task<bool> UpdateStatusAsync(int id, UpdateConnectionStatusRequest request, CancellationToken cancellationToken = default);
}
