using Catalog.Domain.Constants;
using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class SkillDetailConfiguration : IEntityTypeConfiguration<SkillDetail>
{
    public void Configure(EntityTypeBuilder<SkillDetail> builder)
    {
        builder.ToTable("SkillDetails");
        builder.HasKey(d => d.SkillId);                                  // PK = FK (1:1)

        builder.Property(d => d.Description).HasMaxLength(CatalogConstants.DescriptionMaxLength);
        builder.Property(d => d.IconUrl).HasMaxLength(CatalogConstants.IconUrlMaxLength);
        builder.Property(d => d.UpdatedAt).HasColumnType("datetime2");
        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}
