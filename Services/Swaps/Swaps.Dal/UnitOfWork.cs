using System.Data;
using Microsoft.Data.SqlClient;
using Swaps.Dal.Interfaces;
using Swaps.Dal.Repositories;

namespace Swaps.Dal;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
        Swaps = new SwapRepository(_connection, () => _transaction);
        Skills = new SkillRepository(_connection, () => _transaction);
        History = new SwapStatusHistoryRepository(_connection, () => _transaction);
    }

    public ISwapRepository Swaps { get; }
    public ISkillRepository Skills { get; }
    public ISwapStatusHistoryRepository History { get; }

    public async Task BeginTransactionAsync(
        IsolationLevel level = IsolationLevel.ReadCommitted,
        CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction is in progress.");

        try
        {
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;

        try
        {
            if (_transaction.Connection is not null)
                await _transaction.RollbackAsync(ct);
        }
        catch (InvalidOperationException)
        {
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await _connection.DisposeAsync();
    }
}
