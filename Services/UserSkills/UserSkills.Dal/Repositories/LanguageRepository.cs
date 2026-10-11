using System.Data;
using Microsoft.Data.SqlClient;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Models;

namespace UserSkills.Dal.Repositories;

/// <summary>
/// Репозиторій на чистому ADO.NET (без Dapper і без Generic Repository): показує роботу з
/// з'єднанням, параметрами та транзакцією UoW без сторонніх бібліотек.
/// </summary>
public sealed class LanguageRepository : RepositoryBase, ILanguageRepository
{
    public LanguageRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction)
    {
    }

    public async Task<IReadOnlyList<Language>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT Code, Name FROM dbo.Languages ORDER BY Code";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new List<Language>();
        while (await reader.ReadAsync(ct))
            result.Add(new Language { Code = reader.GetString(0), Name = reader.GetString(1) });
        return result;
    }

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = "SELECT Code, Name FROM dbo.Languages WHERE Code = @Code";
        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Code", SqlDbType.NVarChar, 10).Value = code; // лише параметр

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return new Language { Code = reader.GetString(0), Name = reader.GetString(1) };
    }

    public async Task<IReadOnlyList<string>> GetUnknownCodesAsync(
        IReadOnlyCollection<string> codes, CancellationToken ct = default)
    {
        if (codes.Count == 0) return [];

        // Імена параметрів (@c0, @c1, ...) генеруються з індексу; самі значення — лише параметри.
        var names = codes.Select((_, i) => "@c" + i).ToArray();
        var sql = $"SELECT Code FROM dbo.Languages WHERE Code IN ({string.Join(", ", names)})";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        var index = 0;
        foreach (var code in codes)
            command.Parameters.Add(names[index++], SqlDbType.NVarChar, 10).Value = code;

        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            known.Add(reader.GetString(0));

        return codes.Where(c => !known.Contains(c)).ToList();
    }
}
