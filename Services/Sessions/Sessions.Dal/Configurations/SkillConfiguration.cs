using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Models;

namespace Sessions.Dal.Configurations;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    private static readonly DateTime SeedDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(Skill.NameMaxLength);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.IsDeleted).HasDefaultValue(false);
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Skills_Name");

        builder.HasData(
            new { Id = 1, Name = "Програмування C#", CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 2, Name = "Англійська розмовна", CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 3, Name = "Гра на гітарі", CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 4, Name = "UI-дизайн", CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 5, Name = "Фотографія", CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 6, Name = "Приготування випічки", CreatedAt = SeedDate, IsDeleted = false });
    }
}
