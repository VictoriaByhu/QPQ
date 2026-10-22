using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Constants;
using Sessions.Domain.Models;

namespace Sessions.Dal.Configurations;

public class AcceptedSwapConfiguration : IEntityTypeConfiguration<AcceptedSwap>
{
    public void Configure(EntityTypeBuilder<AcceptedSwap> builder)
    {
        builder.ToTable("AcceptedSwaps", t =>
        {
            t.HasCheckConstraint("CK_AcceptedSwaps_Status",
                "[Status] IN (N'Accepted', N'Completed', N'Cancelled')");
            t.HasCheckConstraint("CK_AcceptedSwaps_Parties", "[InitiatorId] <> [PartnerId]");
        });

        builder.HasKey(a => a.SwapId);
        builder.Property(a => a.SwapId).ValueGeneratedNever();
        builder.Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(AcceptedSwap.StatusMaxLength)
            .HasDefaultValue(AcceptedSwapStatuses.Accepted);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasIndex(a => a.InitiatorId).HasDatabaseName("IX_AcceptedSwaps_Initiator");
        builder.HasIndex(a => a.PartnerId).HasDatabaseName("IX_AcceptedSwaps_Partner");
    }
}
