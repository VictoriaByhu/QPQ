using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Dal.Entities;

namespace Sessions.Dal.Configurations;

public class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public const string PrimaryKeyName = "PK_ProcessedEvents";

    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("ProcessedEvents");

        builder.HasKey(e => e.EventId).HasName(PrimaryKeyName);
        builder.Property(e => e.EventId).ValueGeneratedNever();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(ProcessedEvent.EventTypeMaxLength);
        builder.Property(e => e.ProcessedAt).HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
