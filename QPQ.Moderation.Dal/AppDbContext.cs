using Microsoft.EntityFrameworkCore;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Entities;

namespace QPQ.Moderation.Dal;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportDetail> ReportDetails => Set<ReportDetail>();
    public DbSet<ReportAttachment> Attachments => Set<ReportAttachment>();
    public DbSet<Moderator> Moderators => Set<Moderator>();
    public DbSet<ReportAssignment> ReportAssignments => Set<ReportAssignment>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ReportTag> ReportTags => Set<ReportTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Цей рядок автоматично застосує всі налаштування з Кроку 3
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}