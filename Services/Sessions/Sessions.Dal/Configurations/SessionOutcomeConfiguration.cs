using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Models;

namespace Sessions.Dal.Configurations;

public class SessionOutcomeConfiguration : IEntityTypeConfiguration<SessionOutcome>
{
    public void Configure(EntityTypeBuilder<SessionOutcome> builder)
    {
        builder.ToTable("SessionOutcomes", t =>
            t.HasCheckConstraint("CK_SessionOutcomes_Duration", "[ActualDurationMinutes] > 0"));

        builder.HasKey(o => o.SessionId);
        builder.Property(o => o.SessionId).ValueGeneratedNever();
        builder.Property(o => o.Summary).HasMaxLength(SessionOutcome.SummaryMaxLength);
        builder.Property(o => o.CompletedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasQueryFilter(o => !o.Session.IsDeleted);
    }
}
