using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Models;

namespace Sessions.Dal.Configurations;

public class SessionSkillConfiguration : IEntityTypeConfiguration<SessionSkill>
{
    public void Configure(EntityTypeBuilder<SessionSkill> builder)
    {
        builder.ToTable("SessionSkills", t =>
            t.HasCheckConstraint("CK_SessionSkills_Minutes", "[PlannedMinutes] > 0"));

        builder.HasKey(ss => new { ss.SessionId, ss.SkillId });
        builder.Property(ss => ss.PlannedMinutes).HasDefaultValue(30);
        builder.Property(ss => ss.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasQueryFilter(ss => !ss.Session.IsDeleted);

        builder.HasIndex(ss => new { ss.SkillId, ss.SessionId }).HasDatabaseName("IX_SessionSkills_Skill");

        builder.HasOne(ss => ss.Skill)
            .WithMany()
            .HasForeignKey(ss => ss.SkillId)
            .HasConstraintName("FK_SessionSkills_Skill")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
