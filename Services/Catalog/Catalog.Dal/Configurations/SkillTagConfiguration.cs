using Catalog.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

/// <summary>M:N Skill <-> Tag через явну проміжну сутність із власним полем AddedAt.</summary>
public class SkillTagConfiguration : IEntityTypeConfiguration<SkillTag>
{
    public void Configure(EntityTypeBuilder<SkillTag> builder)
    {
        builder.ToTable("SkillTags");
        builder.HasKey(st => new { st.SkillId, st.TagId });              // складений ключ
        builder.Property(st => st.AddedAt).HasColumnType("datetime2").HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(st => st.Skill).WithMany(s => s.SkillTags)
               .HasForeignKey(st => st.SkillId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_SkillTags_Skill");
        builder.HasOne(st => st.Tag).WithMany(t => t.SkillTags)
               .HasForeignKey(st => st.TagId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_SkillTags_Tag");

        builder.HasIndex(st => new { st.TagId, st.SkillId }).HasDatabaseName("IX_SkillTags_Tag");
    }
}
