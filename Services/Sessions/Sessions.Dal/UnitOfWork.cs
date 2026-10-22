using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sessions.Dal.Interfaces;
using Sessions.Dal.Repositories;
using Sessions.Domain.Exceptions;
using Sessions.Domain.Models;

namespace Sessions.Dal;

public class UnitOfWork : IUnitOfWork
{
    private readonly SessionsDbContext _context;

    public UnitOfWork(SessionsDbContext context)
    {
        _context = context;
        Sessions = new SessionRepository(context);
        AcceptedSwaps = new AcceptedSwapRepository(context);
        SessionSkills = new SessionSkillRepository(context);
        Skills = new GenericRepository<Skill>(context);
        Formats = new GenericRepository<SessionFormat>(context);
    }

    public ISessionRepository Sessions { get; }
    public IAcceptedSwapRepository AcceptedSwaps { get; }
    public ISessionSkillRepository SessionSkills { get; }
    public IGenericRepository<Skill> Skills { get; }
    public IGenericRepository<SessionFormat> Formats { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessConflictException("The data was modified by someone else. Reload it and try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessConflictException("A record with the same unique values already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            throw new BusinessConflictException("The change violates a database constraint.");
        }
    }
}
