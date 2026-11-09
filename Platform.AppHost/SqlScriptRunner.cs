using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

/// <summary>
/// Виконує .sql-скрипти з GO-роздільниками (Лаб. 01–02: схема, процедури, сід UserSkills).
/// Усі скрипти ідемпотентні, тому повторний запуск AppHost безпечний.
/// </summary>
internal static partial class SqlScriptRunner
{
    public static async Task RunAsync(string connectionString, IEnumerable<string> files, ILogger logger, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);

        foreach (var file in files)
        {
            var batches = GoSeparator().Split(await File.ReadAllTextAsync(file, ct))
                .Where(b => !string.IsNullOrWhiteSpace(b));

            foreach (var batch in batches)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = batch;
                command.CommandTimeout = 120;
                await command.ExecuteNonQueryAsync(ct);
            }

            logger.LogInformation("Applied {Script}", Path.GetFileName(file));
        }
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}
