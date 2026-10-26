using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    // Для похідних контекстів (наприклад, тестових), які мають власний тип параметрів.
    protected CatalogDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillDetail> SkillDetails => Set<SkillDetail>();
    public DbSet<SkillLevel> SkillLevels => Set<SkillLevel>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SkillTag> SkillTags => Set<SkillTag>();

    // Усі IEntityTypeConfiguration<T> з цієї збірки підключаються однією командою.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
}
