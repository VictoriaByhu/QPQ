using System.Data;
using Microsoft.Data.SqlClient;
using Swaps.Dal.Interfaces;
using Swaps.Domain.Models;

namespace Swaps.Dal.Repositories;

public sealed class SwapStatusHistoryRepository : RepositoryBase, ISwapStatusHistoryRepository
{
    public SwapStatusHistoryRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction)
    {
    }

    public async Task AddAsync(SwapStatusHistoryEntry entry, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.SwapStatusHistory (SwapId, OldStatus, NewStatus, ChangedByUserId, Comment)
            VALUES (@SwapId, @OldStatus, @NewStatus, @ChangedByUserId, @Comment)";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@SwapId", SqlDbType.Int).Value = entry.SwapId;
        command.Parameters.Add("@OldStatus", SqlDbType.NVarChar, 20).Value = (object?)entry.OldStatus ?? DBNull.Value;
        command.Parameters.Add("@NewStatus", SqlDbType.NVarChar, 20).Value = entry.NewStatus;
        command.Parameters.Add("@ChangedByUserId", SqlDbType.UniqueIdentifier).Value = entry.ChangedByUserId;
        command.Parameters.Add("@Comment", SqlDbType.NVarChar, 500).Value = (object?)entry.Comment ?? DBNull.Value;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<SwapStatusHistoryEntry>> GetBySwapAsync(int swapId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT Id, SwapId, OldStatus, NewStatus, ChangedByUserId, Comment, ChangedAt
            FROM dbo.SwapStatusHistory
            WHERE SwapId = @SwapId
            ORDER BY ChangedAt, Id";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@SwapId", SqlDbType.Int).Value = swapId;
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new List<SwapStatusHistoryEntry>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new SwapStatusHistoryEntry
            {
                Id = reader.GetInt64(0),
                SwapId = reader.GetInt32(1),
                OldStatus = reader.IsDBNull(2) ? null : reader.GetString(2),
                NewStatus = reader.GetString(3),
                ChangedByUserId = reader.GetGuid(4),
                Comment = reader.IsDBNull(5) ? null : reader.GetString(5),
                ChangedAt = reader.GetDateTime(6)
            });
        }
        return result;
    }
}
