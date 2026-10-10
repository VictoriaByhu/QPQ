using Dapper;
using Microsoft.Data.SqlClient;
using Swaps.Dal.Interfaces;

namespace Swaps.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    private readonly string _table;
    private readonly string _columns;
    private readonly bool _softDelete;

    protected BaseDapperRepository(
        SqlConnection connection,
        Func<SqlTransaction?> currentTransaction,
        string table,
        string columns,
        bool softDelete = false)
        : base(connection, currentTransaction)
    {
        _table = table;
        _columns = columns;
        _softDelete = softDelete;
    }

    protected abstract IReadOnlyList<string> InsertColumns { get; }

    private string ActiveFilter => _softDelete ? " AND IsDeleted = 0" : string.Empty;

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var sql = $"SELECT {_columns} FROM {_table} WHERE Id = @Id{ActiveFilter}";
        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        var filter = _softDelete ? " WHERE IsDeleted = 0" : string.Empty;
        var sql = $"SELECT {_columns} FROM {_table}{filter} ORDER BY Id";
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<T>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
        return rows.ToList();
    }

    public virtual async Task<int> AddAsync(T entity, CancellationToken ct = default)
    {
        var columnList = string.Join(", ", InsertColumns);
        var parameterList = string.Join(", ", InsertColumns.Select(c => "@" + c));
        var sql = $"INSERT INTO {_table} ({columnList}) OUTPUT INSERTED.Id VALUES ({parameterList})";
        await EnsureOpenAsync(ct);
        try
        {
            return await Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, entity, Transaction, cancellationToken: ct));
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public virtual async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var sql = _softDelete
            ? $"UPDATE {_table} SET IsDeleted = 1, DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE Id = @Id AND IsDeleted = 0"
            : $"DELETE FROM {_table} WHERE Id = @Id";
        await EnsureOpenAsync(ct);
        try
        {
            var affected = await Connection.ExecuteAsync(
                new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
            return affected > 0;
        }
        catch (SqlException ex) when (SqlExceptionTranslator.IsKnown(ex))
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }
}
