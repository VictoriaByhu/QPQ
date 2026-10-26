using Catalog.Domain.Constants;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(CatalogConstants.TagNameMaxLength);
        builder.HasIndex(t => t.Name).IsUnique().HasDatabaseName("UQ_Tags_Name");

        builder.HasData(
            new { Id = 1, Name = ".NET" },
            new { Id = 2, Name = "Практика" },
            new { Id = 3, Name = "Онлайн" },
            new { Id = 4, Name = "Для початківців" },
            new { Id = 5, Name = "Творчість" },
            new { Id = 6, Name = "Мови" });
    }
}
