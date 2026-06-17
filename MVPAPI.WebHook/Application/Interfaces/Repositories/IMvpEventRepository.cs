using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Interfaces.Repositories;

public interface IMvpEventRepository
{
    Task<IReadOnlyList<MvpEvent>> GetByCompanyIdAsync(int companyId, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<MvpEvent?> GetBySeqnoAsync(Guid seqno, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MvpEvent>> GetPendingAsync(int companyId, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(MvpEvent mvpEvent, CancellationToken cancellationToken = default);
}
