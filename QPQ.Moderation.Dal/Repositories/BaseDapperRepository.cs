using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using QPQ.Moderation.Dal.Interfaces;

namespace QPQ.Moderation.Dal.Repositories;

public abstract class BaseDapperRepository<T> : RepositoryBase, IGenericRepository<T> where T : class
{
    private readonly string _tableName;

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction, string tableName)
        : base(connection, currentTransaction)
    {
        _tableName = tableName;
    }

    public async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE Id = @Id AND IsDeleted = 0";
        return await Connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE IsDeleted = 0";
        var result = await Connection.QueryAsync<T>(
            new CommandDefinition(sql, null, Transaction, cancellationToken: ct));
        return result.ToList();
    }

    public virtual Task<long> AddAsync(T entity, CancellationToken ct = default)
    {
        throw new NotImplementedException("Специфічна реалізація в кожному репозиторії");
    }
}