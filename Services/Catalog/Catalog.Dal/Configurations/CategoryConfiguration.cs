using Catalog.Domain.Constants;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Дата початкових даних зафіксована, щоб міграція не змінювалась між запусками.</summary>
    internal static readonly DateTime SeedDate = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t => t.HasCheckConstraint("CK_Categories_Slug",
            "LEN([Slug]) > 0 AND [Slug] COLLATE Latin1_General_BIN2 = LOWER([Slug]) COLLATE Latin1_General_BIN2"));
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(CatalogConstants.NameMaxLength);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(CatalogConstants.SlugMaxLength);
        builder.Property(c => c.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(c => c.UpdatedAt).HasColumnType("datetime2");
        builder.Property(c => c.DeletedAt).HasColumnType("datetime2");
        builder.Property(c => c.RowVersion).IsRowVersion();

        // Унікальність діє лише серед не видалених записів (м'яке видалення).
        builder.HasIndex(c => c.Name).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Categories_Name");
        builder.HasIndex(c => c.Slug).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Categories_Slug");

        // Статичний сідінг довідника: фіксовані Id, збігаються з даними інших контекстів QPQ.
        builder.HasData(
            new { Id = 1, Name = "Програмування", Slug = "programming", SortOrder = 10, CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 2, Name = "Мови",          Slug = "languages",   SortOrder = 20, CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 3, Name = "Музика",        Slug = "music",       SortOrder = 30, CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 4, Name = "Дизайн",        Slug = "design",      SortOrder = 40, CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 5, Name = "Фото",          Slug = "photo",       SortOrder = 50, CreatedAt = SeedDate, IsDeleted = false },
            new { Id = 6, Name = "Кулінарія",     Slug = "cooking",     SortOrder = 60, CreatedAt = SeedDate, IsDeleted = false });
    }
}
