using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Constants;
using Sessions.Domain.Models;

namespace Sessions.Dal.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions", t =>
        {
            t.HasCheckConstraint("CK_Sessions_Duration", "[DurationMinutes] > 0");
            t.HasCheckConstraint("CK_Sessions_Status",
                "[Status] IN (N'Planned', N'Completed', N'Cancelled')");
            t.HasCheckConstraint("CK_Sessions_Place",
                "([FormatId] = 1 AND [Link] IS NOT NULL) OR ([FormatId] = 2 AND [Location] IS NOT NULL)");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).UseIdentityColumn();
        builder.Property(s => s.FormatId).HasColumnType("tinyint");
        builder.Property(s => s.DurationMinutes).HasDefaultValue(60);
        builder.Property(s => s.Link).HasMaxLength(Session.LinkMaxLength);
        builder.Property(s => s.Location).HasMaxLength(Session.LocationMaxLength);
        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(Session.StatusMaxLength)
            .HasDefaultValue(SessionStatuses.Planned);
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(s => s.IsDeleted).HasDefaultValue(false);
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Ignore(s => s.PlannedSkillMinutes);

        builder.HasQueryFilter(s => !s.IsDeleted);

        builder.HasIndex(s => new { s.SwapId, s.ScheduledAt })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_Sessions_Swap_Time");

        builder.HasOne(s => s.Swap)
            .WithMany(a => a.Sessions)
            .HasForeignKey(s => s.SwapId)
            .HasConstraintName("FK_Sessions_AcceptedSwap")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Format)
            .WithMany()
            .HasForeignKey(s => s.FormatId)
            .HasConstraintName("FK_Sessions_Format")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Outcome)
            .WithOne(o => o.Session)
            .HasForeignKey<SessionOutcome>(o => o.SessionId)
            .HasConstraintName("FK_SessionOutcomes_Session")
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasMany(s => s.Skills)
            .WithOne(ss => ss.Session)
            .HasForeignKey(ss => ss.SessionId)
            .HasConstraintName("FK_SessionSkills_Session")
            .OnDelete(DeleteBehavior.ClientCascade);
    }
}
