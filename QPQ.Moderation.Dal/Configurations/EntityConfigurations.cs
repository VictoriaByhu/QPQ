using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QPQ.Moderation.Domain;
using QPQ.Moderation.Domain.Entities;

namespace QPQ.Moderation.Dal.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);

        builder.HasOne(r => r.Detail).WithOne()
            .HasForeignKey<ReportDetail>(d => d.ReportId);

        builder.HasMany(r => r.Attachments).WithOne()
            .HasForeignKey(a => a.ReportId);
    }
}

public class ReportAssignmentConfiguration : IEntityTypeConfiguration<ReportAssignment>
{
    public void Configure(EntityTypeBuilder<ReportAssignment> builder)
    {
        builder.ToTable("ReportAssignments");
        builder.HasKey(ra => new { ra.ReportId, ra.ModeratorId });
        builder.Property(ra => ra.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public class ModeratorConfiguration : IEntityTypeConfiguration<Moderator>
{
    public void Configure(EntityTypeBuilder<Moderator> builder)
    {
        builder.ToTable("Moderators");
        builder.HasKey(m => m.Id);

        // Використовуємо Username замість Name
        builder.HasData(
    new { Id = 1L, Username = "Admin", Email = "admin@test.com" },
    new { Id = 2L, Username = "Support", Email = "support@test.com" }
);

    }
}

public class ReportDetailConfiguration : IEntityTypeConfiguration<ReportDetail>
{
    public void Configure(EntityTypeBuilder<ReportDetail> builder)
    {
        builder.HasKey(d => d.ReportId);
    }
}
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
    }
}

public class ReportTagConfiguration : IEntityTypeConfiguration<ReportTag>
{
    public void Configure(EntityTypeBuilder<ReportTag> builder)
    {
        builder.ToTable("ReportTags");

        // Складений первинний ключ для M:N
        builder.HasKey(rt => new { rt.ReportId, rt.TagId });
        builder.Property(rt => rt.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}