using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoProcessingJob
/// </summary>
public class VideoProcessingJobConfiguration : IEntityTypeConfiguration<VideoProcessingJob>
{
    public void Configure(EntityTypeBuilder<VideoProcessingJob> builder)
    {
        builder.ToTable("VideoProcessingJobs");

        builder.HasKey(vpj => vpj.Id);

        builder.Property(vpj => vpj.JobType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vpj => vpj.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vpj => vpj.Progress)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(vpj => vpj.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(vpj => vpj.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vpj => vpj.VideoId)
            .HasDatabaseName("IX_VideoProcessingJobs_VideoId");

        builder.HasIndex(vpj => vpj.Status)
            .HasDatabaseName("IX_VideoProcessingJobs_Status");

        builder.HasIndex(vpj => new { vpj.VideoId, vpj.JobType })
            .HasDatabaseName("IX_VideoProcessingJobs_VideoId_JobType");

        // Relationships
        builder.HasOne(vpj => vpj.Video)
            .WithMany(v => v.VideoProcessingJobs)
            .HasForeignKey(vpj => vpj.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
