using Catalog.Domain.Constants;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills", t => t.HasCheckConstraint("CK_Skills_Slug",
            "LEN([Slug]) > 0 AND [Slug] COLLATE Latin1_General_BIN2 = LOWER([Slug]) COLLATE Latin1_General_BIN2"));
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(CatalogConstants.NameMaxLength);
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(CatalogConstants.SlugMaxLength);
        builder.Property(s => s.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.UpdatedAt).HasColumnType("datetime2");
        builder.Property(s => s.DeletedAt).HasColumnType("datetime2");
        builder.Property(s => s.RowVersion).IsRowVersion();

        // Назва навички унікальна в усьому довіднику: локальні копії в інших базах мають UNIQUE по Name.
        builder.HasIndex(s => s.Name).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Skills_Name");
        builder.HasIndex(s => s.Slug).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Skills_Slug");
        builder.HasIndex(s => s.CategoryId).HasFilter("[IsDeleted] = 0").HasDatabaseName("IX_Skills_Category")
               .IncludeProperties(s => new { s.Name, s.Slug });

        builder.HasOne(s => s.Category).WithMany(c => c.Skills)          // 1:N
               .HasForeignKey(s => s.CategoryId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Skills_Category");

        builder.HasOne(s => s.Detail).WithOne()                          // 1:1, PK є водночас FK
               .HasForeignKey<SkillDetail>(d => d.SkillId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_SkillDetails_Skill");

        builder.HasMany(s => s.Levels).WithOne()                         // 1:N
               .HasForeignKey(l => l.SkillId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_SkillLevels_Skill");

        // Ідентифікатори навичок 1-6 збігаються з копіями в UserSkills, Swaps і Sessions.
        builder.HasData(
            new { Id = 1, CategoryId = 1, Name = "Програмування C#",     Slug = "csharp",           CreatedAt = CategoryConfiguration.SeedDate, IsDeleted = false },
            new { Id = 2, CategoryId = 2, Name = "Англійська розмовна",  Slug = "english-speaking", CreatedAt = CategoryConfiguration.SeedDate, IsDeleted = false },
            new { Id = 3, CategoryId = 3, Name = "Гра на гітарі",        Slug = "guitar",           CreatedAt = CategoryConfiguration.SeedDate, IsDeleted = false },
            new { Id = 4, CategoryId = 4, Name = "UI-дизайн",            Slug = "ui-design",        CreatedAt = CategoryConfiguration.SeedDate, IsDeleted = false },
            new { Id = 5, CategoryId = 5, Name = "Фотографія",           Slug = "photography",      CreatedAt = CategoryConfiguration.SeedDate, IsDeleted = false },
            new { Id = 6, CategoryId = 6, Name = "Приготування випічки", Slug = "baking",           CreatedAt = CategoryConfiguration.SeedDate, IsDeleted = false });
    }
}
