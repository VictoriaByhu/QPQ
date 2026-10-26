using Catalog.Domain.Constants;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class SkillLevelConfiguration : IEntityTypeConfiguration<SkillLevel>
{
    public void Configure(EntityTypeBuilder<SkillLevel> builder)
    {
        builder.ToTable("SkillLevels", t => t.HasCheckConstraint("CK_SkillLevels_Level", "[Level] BETWEEN 1 AND 5"));
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Level).HasColumnType("tinyint");
        builder.Property(l => l.Title).IsRequired().HasMaxLength(CatalogConstants.LevelTitleMaxLength);
        builder.Property(l => l.Description).HasMaxLength(CatalogConstants.LevelDescriptionMaxLength);

        builder.HasIndex(l => new { l.SkillId, l.Level }).IsUnique().HasDatabaseName("UQ_SkillLevels_Skill_Level");
    }
}
