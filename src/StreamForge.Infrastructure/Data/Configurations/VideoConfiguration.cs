using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Video
/// </summary>
public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        builder.ToTable("Videos");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.Description)
            .HasColumnType("text");

        builder.Property(v => v.Visibility)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(VideoStatus.Ready);

        builder.Property(v => v.AllowComments)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(v => v.AllowLikes)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(v => v.Autoplay)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.Loop)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.DefaultVolume)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(v => v.CaptionsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(v => v.PlayerTheme)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("default");

        builder.Property(v => v.ViewCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(v => v.UploaderId)
            .HasDatabaseName("IX_Videos_UploaderId");

        builder.HasIndex(v => v.CategoryId)
            .HasDatabaseName("IX_Videos_CategoryId");

        builder.HasIndex(v => v.Visibility)
            .HasDatabaseName("IX_Videos_Visibility");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("IX_Videos_Status");

        builder.HasIndex(v => v.CreatedAt)
            .HasDatabaseName("IX_Videos_CreatedAt");

        builder.HasIndex(v => v.ViewCount)
            .HasDatabaseName("IX_Videos_ViewCount");

        // Relationships
        builder.HasOne(v => v.Uploader)
            .WithMany(u => u.Videos)
            .HasForeignKey(v => v.UploaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.VideoVersions)
            .WithOne(vv => vv.Video)
            .HasForeignKey(vv => vv.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VideoThumbnails)
            .WithOne(vt => vt.Video)
            .HasForeignKey(vt => vt.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VideoProcessingJobs)
            .WithOne(vpj => vpj.Video)
            .HasForeignKey(vpj => vpj.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VideoTranscriptions)
            .WithOne(vt => vt.Video)
            .HasForeignKey(vt => vt.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VideoTags)
            .WithOne(vt => vt.Video)
            .HasForeignKey(vt => vt.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VideoReactions)
            .WithOne(vr => vr.Video)
            .HasForeignKey(vr => vr.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.VideoComments)
            .WithOne(vc => vc.Video)
            .HasForeignKey(vc => vc.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Bookmarks)
            .WithOne(b => b.Video)
            .HasForeignKey(b => b.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.AccessControls)
            .WithOne(ac => ac.Video)
            .HasForeignKey(ac => ac.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.AnalyticsEvents)
            .WithOne(ae => ae.Video)
            .HasForeignKey(ae => ae.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Notifications)
            .WithOne(n => n.Video)
            .HasForeignKey(n => n.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
