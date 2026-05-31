using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoReaction
/// </summary>
public class VideoReactionConfiguration : IEntityTypeConfiguration<VideoReaction>
{
    public void Configure(EntityTypeBuilder<VideoReaction> builder)
    {
        builder.ToTable("VideoReactions");

        builder.HasKey(vr => vr.Id);

        builder.Property(vr => vr.ReactionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(vr => vr.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vr => new { vr.VideoId, vr.UserId })
            .IsUnique()
            .HasDatabaseName("IX_VideoReactions_VideoId_UserId");

        builder.HasIndex(vr => vr.UserId)
            .HasDatabaseName("IX_VideoReactions_UserId");

        builder.HasIndex(vr => new { vr.VideoId, vr.ReactionType })
            .HasDatabaseName("IX_VideoReactions_VideoId_ReactionType");

        // Relationships
        builder.HasOne(vr => vr.Video)
            .WithMany(v => v.VideoReactions)
            .HasForeignKey(vr => vr.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vr => vr.User)
            .WithMany(u => u.VideoReactions)
            .HasForeignKey(vr => vr.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
