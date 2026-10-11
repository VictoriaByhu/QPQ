using Dapper;
using Microsoft.Data.SqlClient;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Models;

namespace UserSkills.Dal.Repositories;

/// <summary>Журнал змін статусу (Dapper). Ключ — bigint, тому Generic Repository не використовується.</summary>
public sealed class UserSkillStatusHistoryRepository : RepositoryBase, IUserSkillStatusHistoryRepository
{
    public UserSkillStatusHistoryRepository(
        SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction)
    {
    }

    public async Task<IReadOnlyList<UserSkillStatusHistoryEntry>> GetByUserSkillAsync(
        int userSkillId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, UserSkillId, OldStatus, NewStatus, ChangedByUserId, Comment, ChangedAt
            FROM dbo.UserSkillStatusHistory
            WHERE UserSkillId = @UserSkillId
            ORDER BY ChangedAt, Id";

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<UserSkillStatusHistoryEntry>(
            new CommandDefinition(sql, new { UserSkillId = userSkillId }, Transaction,
                cancellationToken: ct));
        return rows.ToList();
    }
}
