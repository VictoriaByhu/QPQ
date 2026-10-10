using Dapper;
using Microsoft.Data.SqlClient;
using Swaps.Dal.Interfaces;
using Swaps.Domain.Models;

namespace Swaps.Dal.Repositories;

public sealed class SkillRepository : BaseDapperRepository<Skill>, ISkillRepository
{
    private static readonly string[] SkillInsertColumns = ["Id", "Name"];

    public SkillRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Skills", "Id, Name", softDelete: true)
    {
    }

    protected override IReadOnlyList<string> InsertColumns => SkillInsertColumns;

    public async Task<IReadOnlyList<Skill>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Name FROM dbo.Skills WHERE Id IN @Ids AND IsDeleted = 0 ORDER BY Id";
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Skill>(
            new CommandDefinition(sql, new { Ids = ids.ToArray() }, Transaction, cancellationToken: ct));
        return rows.ToList();
    }
}
