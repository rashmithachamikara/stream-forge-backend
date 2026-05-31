using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoTranscription
/// </summary>
public class VideoTranscriptionConfiguration : IEntityTypeConfiguration<VideoTranscription>
{
    public void Configure(EntityTypeBuilder<VideoTranscription> builder)
    {
        builder.ToTable("VideoTranscriptions");

        builder.HasKey(vt => vt.Id);

        builder.Property(vt => vt.Language)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(vt => vt.StoragePath)
            .HasMaxLength(2000);

        builder.Property(vt => vt.Format)
            .HasMaxLength(50);

        builder.Property(vt => vt.Source)
            .HasMaxLength(100);

        builder.Property(vt => vt.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vt => vt.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vt => vt.VideoId)
            .HasDatabaseName("IX_VideoTranscriptions_VideoId");

        builder.HasIndex(vt => new { vt.VideoId, vt.Language })
            .IsUnique()
            .HasDatabaseName("IX_VideoTranscriptions_VideoId_Language");

        // Relationships
        builder.HasOne(vt => vt.Video)
            .WithMany(v => v.VideoTranscriptions)
            .HasForeignKey(vt => vt.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
