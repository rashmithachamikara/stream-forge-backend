using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoThumbnail
/// </summary>
public class VideoThumbnailConfiguration : IEntityTypeConfiguration<VideoThumbnail>
{
    public void Configure(EntityTypeBuilder<VideoThumbnail> builder)
    {
        builder.ToTable("VideoThumbnails");

        builder.HasKey(vt => vt.Id);

        builder.Property(vt => vt.StoragePath)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(vt => vt.Width)
            .IsRequired();

        builder.Property(vt => vt.Height)
            .IsRequired();

        builder.Property(vt => vt.TimestampSeconds);

        builder.Property(vt => vt.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(vt => vt.SizeBytes)
            .IsRequired();

        builder.Property(vt => vt.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vt => vt.VideoId)
            .HasDatabaseName("IX_VideoThumbnails_VideoId");

        builder.HasIndex(vt => new { vt.VideoId, vt.IsDefault })
            .HasDatabaseName("IX_VideoThumbnails_VideoId_IsDefault");

        // Relationships
        builder.HasOne(vt => vt.Video)
            .WithMany(v => v.VideoThumbnails)
            .HasForeignKey(vt => vt.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
