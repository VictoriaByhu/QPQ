using System.Data;
using Microsoft.Data.SqlClient;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Dal.Repositories;

namespace QPQ.Moderation.Dal;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
        // Усі репозиторії отримують ОДНЕ підключення і доступ до поточної транзакції
        Moderators = new ModeratorRepository(_connection, () => _transaction);
        Reports = new ReportRepository(_connection, () => _transaction);
        ViolationTypes = new ViolationTypeRepository(_connection, () => _transaction);
    }

    public IModeratorRepository Moderators { get; }
    public IReportRepository Reports { get; }
    public IViolationTypeRepository ViolationTypes { get; }

    public async Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_connection.State != ConnectionState.Open) await _connection.OpenAsync(ct);
        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null) await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}