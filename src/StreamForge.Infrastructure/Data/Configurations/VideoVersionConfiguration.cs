using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoVersion
/// </summary>
public class VideoVersionConfiguration : IEntityTypeConfiguration<VideoVersion>
{
    public void Configure(EntityTypeBuilder<VideoVersion> builder)
    {
        builder.ToTable("VideoVersions");

        builder.HasKey(vv => vv.Id);

        builder.Property(vv => vv.Resolution)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(vv => vv.Format)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vv => vv.Bitrate);

        builder.Property(vv => vv.Codec)
            .HasMaxLength(100);

        builder.Property(vv => vv.StoragePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(vv => vv.SizeBytes)
            .IsRequired();

        builder.Property(vv => vv.DurationSeconds)
            .IsRequired();

        builder.Property(vv => vv.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vv => vv.VideoId)
            .HasDatabaseName("IX_VideoVersions_VideoId");

        builder.HasIndex(vv => vv.Resolution)
            .HasDatabaseName("IX_VideoVersions_Resolution");

        // Relationships
        builder.HasOne(vv => vv.Video)
            .WithMany(v => v.VideoVersions)
            .HasForeignKey(vv => vv.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(vv => vv.Files)
            .WithOne(vf => vf.VideoVersion)
            .HasForeignKey(vf => vf.VideoVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
