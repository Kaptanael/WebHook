using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Events;

namespace MVPAPI.WebHook.Application.Interfaces.Services;

public interface IMvpEventService
{
    Task<Result<IReadOnlyList<MvpEventResponse>>> GetByClientTokenAsync(string clientToken, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<MvpEventResponse?> GetBySeqnoAsync(Guid seqno, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MvpEventResponse>>> GetPendingByClientTokenAsync(string clientToken, CancellationToken cancellationToken = default);
    Task<Result<MvpEventResponse>> CreateAsync(string clientToken, CreateMVPEventPayload payload, CancellationToken cancellationToken = default);
}
