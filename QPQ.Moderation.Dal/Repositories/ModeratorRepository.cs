using System.Data;
using Microsoft.Data.SqlClient;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal.Repositories;

public sealed class ModeratorRepository : IModeratorRepository
{
    private readonly SqlConnection _connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    public ModeratorRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        _connection = connection;
        _currentTransaction = currentTransaction;
    }

    public async Task<Moderator?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = "SELECT Id, Username, Email FROM dbo.Moderators WHERE Id = @Id AND IsDeleted = 0";
        if (_connection.State != ConnectionState.Open) await _connection.OpenAsync(ct);

        await using var command = new SqlCommand(sql, _connection, _currentTransaction());
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id; // Захист від ін'єкцій

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;

        return new Moderator
        {
            Id = reader.GetInt64(0),
            Username = reader.GetString(1),
            Email = reader.GetString(2)
        };
    }
}