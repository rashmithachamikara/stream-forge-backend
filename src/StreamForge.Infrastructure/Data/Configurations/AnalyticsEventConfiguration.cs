using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AnalyticsEvent
/// </summary>
public class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
    {
        builder.ToTable("AnalyticsEvents");

        builder.HasKey(ae => ae.Id);

        builder.Property(ae => ae.SessionId)
            .IsRequired();

        builder.Property(ae => ae.EventType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ae => ae.EventTime)
            .IsRequired();

        builder.Property(ae => ae.Position);

        builder.Property(ae => ae.DurationWatched);

        builder.Property(ae => ae.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(ae => ae.UserAgent)
            .HasMaxLength(500);

        builder.Property(ae => ae.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ae => ae.VideoId)
            .HasDatabaseName("IX_AnalyticsEvents_VideoId");

        builder.HasIndex(ae => ae.UserId)
            .HasDatabaseName("IX_AnalyticsEvents_UserId");

        builder.HasIndex(ae => ae.SessionId)
            .HasDatabaseName("IX_AnalyticsEvents_SessionId");

        builder.HasIndex(ae => ae.EventType)
            .HasDatabaseName("IX_AnalyticsEvents_EventType");

        builder.HasIndex(ae => ae.CreatedAt)
            .HasDatabaseName("IX_AnalyticsEvents_CreatedAt");

        builder.HasIndex(ae => new { ae.VideoId, ae.EventTime })
            .HasDatabaseName("IX_AnalyticsEvents_VideoId_EventTime");

        // Relationships
        builder.HasOne(ae => ae.Video)
            .WithMany(v => v.AnalyticsEvents)
            .HasForeignKey(ae => ae.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ae => ae.User)
            .WithMany()
            .HasForeignKey(ae => ae.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
