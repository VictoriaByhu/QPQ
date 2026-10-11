using Microsoft.Data.SqlClient;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Models;

namespace UserSkills.Dal.Repositories;

/// <summary>Читання локальної копії довідника навичок (Dapper + BaseDapperRepository).</summary>
public sealed class SkillRepository : BaseDapperRepository<Skill>, ISkillRepository
{
    public SkillRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction,
            table: "dbo.Skills",
            columns: "Id, Name, CreatedAt, UpdatedAt, RowVersion",
            softDelete: true)
    {
    }

    // Id навички видає Catalog (не IDENTITY), тому він входить у список стовпців вставки.
    protected override IReadOnlyList<string> InsertColumns { get; } = ["Id", "Name"];
}
