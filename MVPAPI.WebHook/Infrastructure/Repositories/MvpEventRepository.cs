using Dapper;
using MVPAPI.WebHook.Application.Interfaces.Repositories;
using MVPAPI.WebHook.Domain.Entities;
using MVPAPI.WebHook.Infrastructure.Persistence;

namespace MVPAPI.WebHook.Infrastructure.Repositories;

public class MvpEventRepository(IMvpEventDbConnectionFactory connectionFactory) : IMvpEventRepository
{
    private const string Columns = """
        Seqno, Priority, Cat, PnlNo, EDate, DeviceNo, Status, Facno, Badge,
        Class, Description, Name, Arch, AckOpr, AckTStamp, Actions, RespReq,
        caObjectID, Tag, HasPhoto, HasVideo, Pending, UTCOffset, Sphere,
        CompanyId, SeqNoFromLock, RecordCount
        """;

    public async Task<IReadOnlyList<MvpEvent>> GetByCompanyIdAsync(int companyId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var results = await connection.QueryAsync<MvpEvent>(new CommandDefinition(
            $"""
            SELECT TOP (@PageSize) {Columns}
            FROM [Event]
            WHERE CompanyId = @CompanyId
            ORDER BY EDate DESC
            """,
            new { CompanyId = companyId, PageSize = pageSize },
            cancellationToken: cancellationToken));
        return results.ToList();
    }

    public async Task<MvpEvent?> GetBySeqnoAsync(Guid seqno, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<MvpEvent>(new CommandDefinition(
            $"""
            SELECT {Columns}
            FROM [Event]
            WHERE Seqno = @Seqno
            """,
            new { Seqno = seqno },
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<MvpEvent>> GetPendingAsync(int companyId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var results = await connection.QueryAsync<MvpEvent>(new CommandDefinition(
            $"""
            SELECT {Columns}
            FROM [Event]
            WHERE CompanyId = @CompanyId AND Pending = 1
            ORDER BY EDate DESC
            """,
            new { CompanyId = companyId },
            cancellationToken: cancellationToken));
        return results.ToList();
    }

    public async Task<Guid> AddAsync(MvpEvent mvpEvent, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO [Event]
                (Seqno, Priority, Cat, PnlNo, EDate, DeviceNo, Status, Facno, Badge,
                 Class, Description, Name, Arch, AckOpr, AckTStamp, Actions, RespReq,
                 caObjectID, Tag, HasPhoto, HasVideo, Pending, UTCOffset, Sphere,
                 CompanyId, SeqNoFromLock, RecordCount)
            OUTPUT inserted.Seqno
            VALUES
                (@Seqno, @Priority, @Cat, @PnlNo, @EDate, @DeviceNo, @Status, @Facno, @Badge,
                 @Class, @Description, @Name, @Arch, @AckOpr, @AckTStamp, @Actions, @RespReq,
                 @CaObjectID, @Tag, @HasPhoto, @HasVideo, @Pending, @UTCOffset, @Sphere,
                 @CompanyId, @SeqNoFromLock, @RecordCount)
            """;

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var seqno = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
            sql, mvpEvent, cancellationToken: cancellationToken));
        mvpEvent.Seqno = seqno;
        return seqno;
    }
}
