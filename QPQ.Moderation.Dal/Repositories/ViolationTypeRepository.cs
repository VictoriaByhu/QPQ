using Microsoft.Data.SqlClient;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain;
using System.Data;

namespace QPQ.Moderation.Dal.Repositories;

public sealed class ViolationTypeRepository : RepositoryBase, IViolationTypeRepository
{
    public ViolationTypeRepository(SqlConnection c, Func<SqlTransaction?> tx) : base(c, tx) { }

    public async Task<ViolationType?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Name, Description FROM dbo.ViolationTypes WHERE Id = @Id";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        await using var reader = await command.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        return new ViolationType
        {
            Id = reader.GetInt64(0),
            Name = reader.GetString(1),
            Description = reader.GetString(2)
        };
    }
}