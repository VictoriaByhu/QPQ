using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Swaps.Dal.Interfaces;
using Swaps.Domain.Models;

namespace Swaps.Dal.Repositories;

public sealed class SwapRepository : BaseDapperRepository<Swap>, ISwapRepository
{
    private static readonly string[] SwapInsertColumns = ["InitiatorId", "PartnerId", "Status", "CreatedBy"];

    public SwapRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(
            connection,
            currentTransaction,
            "dbo.Swaps",
            "Id, InitiatorId, PartnerId, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, RowVersion",
            softDelete: true)
    {
    }

    protected override IReadOnlyList<string> InsertColumns => SwapInsertColumns;

    public async Task<Swap?> GetWithDetailsAsync(int swapId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT s.Id, s.InitiatorId, s.PartnerId, s.Status, s.CreatedAt, s.CreatedBy,
                   s.UpdatedAt, s.UpdatedBy, s.RowVersion,
                   d.SwapId, d.Message, d.Location, d.ProposedDate, d.DurationMinutes,
                   ss.SkillId, sk.Name AS SkillName, ss.Role
            FROM dbo.Swaps s
            LEFT JOIN dbo.SwapDetails d ON d.SwapId = s.Id
            LEFT JOIN dbo.SwapSkills ss ON ss.SwapId = s.Id
            LEFT JOIN dbo.Skills sk ON sk.Id = ss.SkillId
            WHERE s.Id = @SwapId AND s.IsDeleted = 0
            ORDER BY ss.Role, ss.SkillId";

        await EnsureOpenAsync(ct);
        Swap? result = null;
        await Connection.QueryAsync<Swap, SwapDetails?, SwapSkill?, Swap>(
            new CommandDefinition(sql, new { SwapId = swapId }, Transaction, cancellationToken: ct),
            (swap, details, skill) =>
            {
                result ??= swap;
                result.Details ??= details;
                if (skill is not null)
                {
                    skill.SwapId = result.Id;
                    result.Skills.Add(skill);
                }
                return result;
            },
            splitOn: "SwapId,SkillId");
        return result;
    }

    public async Task<IReadOnlyList<Swap>> GetForUserAsync(
        Guid userId, string? role, string? status, int page, int pageSize,
        CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Swap>(new CommandDefinition(
            "dbo.usp_GetUserSwaps",
            new { UserId = userId, Role = role, Status = status, PageNumber = page, PageSize = pageSize },
            Transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
        return rows.ToList();
    }

    public async Task AddDetailsAsync(SwapDetails details, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.SwapDetails (SwapId, Message, Location, ProposedDate, DurationMinutes)
            VALUES (@SwapId, @Message, @Location, @ProposedDate, @DurationMinutes)";
        await EnsureOpenAsync(ct);
        await Connection.ExecuteAsync(new CommandDefinition(sql, details, Transaction, cancellationToken: ct));
    }

    public async Task AddSkillAsync(SwapSkill skill, CancellationToken ct = default)
    {
        const string sql = "INSERT INTO dbo.SwapSkills (SwapId, SkillId, Role) VALUES (@SwapId, @SkillId, @Role)";
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(sql, skill, Transaction, cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> UpdateDetailsAsync(SwapDetails details, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.SwapDetails
            SET Message = @Message, Location = @Location, ProposedDate = @ProposedDate,
                DurationMinutes = @DurationMinutes, UpdatedAt = SYSUTCDATETIME()
            WHERE SwapId = @SwapId";
        await EnsureOpenAsync(ct);
        var affected = await Connection.ExecuteAsync(
            new CommandDefinition(sql, details, Transaction, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<bool> TouchAsync(int swapId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Swaps
            SET UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UserId
            WHERE Id = @SwapId AND IsDeleted = 0 AND RowVersion = @RowVersion";
        await EnsureOpenAsync(ct);
        var affected = await Connection.ExecuteAsync(new CommandDefinition(
            sql, new { SwapId = swapId, UserId = userId, RowVersion = rowVersion },
            Transaction, cancellationToken: ct));
        return affected > 0;
    }

    public async Task<byte[]> ChangeStatusAsync(
        int swapId, string newStatus, Guid userId, byte[] rowVersion, string? comment,
        CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            return await Connection.QuerySingleAsync<byte[]>(new CommandDefinition(
                "dbo.usp_ChangeSwapStatus",
                new
                {
                    SwapId = swapId,
                    NewStatus = newStatus,
                    ChangedByUserId = userId,
                    RowVersion = rowVersion,
                    Comment = comment
                },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task SoftDeleteAsync(int swapId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_SoftDeleteSwap",
                new { SwapId = swapId, DeletedByUserId = userId, RowVersion = rowVersion },
                Transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }
}
