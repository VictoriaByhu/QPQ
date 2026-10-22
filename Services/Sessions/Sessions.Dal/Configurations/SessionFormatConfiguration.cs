using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sessions.Domain.Constants;
using Sessions.Domain.Models;

namespace Sessions.Dal.Configurations;

public class SessionFormatConfiguration : IEntityTypeConfiguration<SessionFormat>
{
    public void Configure(EntityTypeBuilder<SessionFormat> builder)
    {
        builder.ToTable("SessionFormats");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnType("tinyint").ValueGeneratedNever();
        builder.Property(f => f.Code).IsRequired().HasMaxLength(SessionFormat.CodeMaxLength);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(SessionFormat.NameMaxLength);

        builder.HasIndex(f => f.Code).IsUnique().HasDatabaseName("UQ_SessionFormats_Code");

        builder.HasData(
            new SessionFormat(SessionFormatIds.Online, "Online", "Онлайн"),
            new SessionFormat(SessionFormatIds.InPerson, "InPerson", "Особиста зустріч"));
    }
}
