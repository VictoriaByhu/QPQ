using Microsoft.EntityFrameworkCore;
using Sessions.Domain.Models;

namespace Sessions.Dal;

public class SessionsDbContext(DbContextOptions<SessionsDbContext> options) : DbContext(options)
{
    public DbSet<SessionFormat> SessionFormats => Set<SessionFormat>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<AcceptedSwap> AcceptedSwaps => Set<AcceptedSwap>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionSkill> SessionSkills => Set<SessionSkill>();
    public DbSet<SessionOutcome> SessionOutcomes => Set<SessionOutcome>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(SessionsDbContext).Assembly);
}
