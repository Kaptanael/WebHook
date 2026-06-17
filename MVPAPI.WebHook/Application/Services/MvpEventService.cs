using AutoMapper;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.DTOs.Events;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Domain.Entities;

namespace MVPAPI.WebHook.Application.Services;

public class MvpEventService(
    IWebHookConnectionRepository connectionRepository,
    IMvpEventRepository mvpEventRepository,
    IMapper mapper) : IMvpEventService
{
    public async Task<Result<IReadOnlyList<MvpEventResponse>>> GetByClientTokenAsync(
        string clientToken, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(clientToken, cancellationToken);
        if (connection is null)
            return Result.Failure<IReadOnlyList<MvpEventResponse>>("No connection found for the supplied client token.");

        var events = await mvpEventRepository.GetByCompanyIdAsync(connection.CompanyId, pageSize, cancellationToken);
        return Result.Success<IReadOnlyList<MvpEventResponse>>(mapper.Map<List<MvpEventResponse>>(events));
    }

    public async Task<MvpEventResponse?> GetBySeqnoAsync(Guid seqno, CancellationToken cancellationToken = default)
    {
        var mvpEvent = await mvpEventRepository.GetBySeqnoAsync(seqno, cancellationToken);
        return mvpEvent is null ? null : mapper.Map<MvpEventResponse>(mvpEvent);
    }

    public async Task<Result<IReadOnlyList<MvpEventResponse>>> GetPendingByClientTokenAsync(
        string clientToken, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(clientToken, cancellationToken);
        if (connection is null)
            return Result.Failure<IReadOnlyList<MvpEventResponse>>("No connection found for the supplied client token.");

        var events = await mvpEventRepository.GetPendingAsync(connection.CompanyId, cancellationToken);
        return Result.Success<IReadOnlyList<MvpEventResponse>>(mapper.Map<List<MvpEventResponse>>(events));
    }

    public async Task<Result<MvpEventResponse>> CreateAsync(
        string clientToken, CreateMVPEventPayload payload, CancellationToken cancellationToken = default)
    {
        var connection = await connectionRepository.GetByClientTokenAsync(clientToken, cancellationToken);
        if (connection is null)
            return Result.Failure<MvpEventResponse>("No connection found for the supplied client token.");

        var mvpEvent = mapper.Map<MvpEvent>(payload);
        mvpEvent.Seqno = Guid.NewGuid();
        mvpEvent.CompanyId = connection.CompanyId;

        await mvpEventRepository.AddAsync(mvpEvent, cancellationToken);
        return Result.Success(mapper.Map<MvpEventResponse>(mvpEvent));
    }
}
