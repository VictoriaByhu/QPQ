using System.Data;
using Microsoft.Data.SqlClient;
using UserSkills.Dal.Interfaces;
using UserSkills.Dal.Repositories;

namespace UserSkills.Dal;

/// <summary>
/// Відкриває одне з'єднання на всю операцію, починає транзакцію з потрібним рівнем ізоляції,
/// координує кілька репозиторіїв в одній транзакції і гарантує Commit або Rollback.
/// Реєструється як Scoped: один екземпляр і одне з'єднання на HTTP-запит.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);

        // Усі репозиторії отримують ОДНЕ з'єднання і доступ до ПОТОЧНОЇ транзакції.
        Skills = new SkillRepository(_connection, () => _transaction);
        Languages = new LanguageRepository(_connection, () => _transaction);
        UserSkills = new UserSkillRepository(_connection, () => _transaction);
        StatusHistory = new UserSkillStatusHistoryRepository(_connection, () => _transaction);
    }

    public ISkillRepository Skills { get; }
    public ILanguageRepository Languages { get; }
    public IUserSkillRepository UserSkills { get; }
    public IUserSkillStatusHistoryRepository StatusHistory { get; }

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
            // Процедури мають власний BEGIN/ROLLBACK; після їхнього відкату транзакція вже
            // завершена на сервері, і повторний Rollback кидає InvalidOperationException.
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
