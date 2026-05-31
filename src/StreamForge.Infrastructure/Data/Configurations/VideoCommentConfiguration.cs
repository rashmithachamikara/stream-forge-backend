using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoComment
/// </summary>
public class VideoCommentConfiguration : IEntityTypeConfiguration<VideoComment>
{
    public void Configure(EntityTypeBuilder<VideoComment> builder)
    {
        builder.ToTable("VideoComments");

        builder.HasKey(vc => vc.Id);

        builder.Property(vc => vc.Comment)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(vc => vc.IsEdited)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(vc => vc.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vc => vc.VideoId)
            .HasDatabaseName("IX_VideoComments_VideoId");

        builder.HasIndex(vc => vc.UserId)
            .HasDatabaseName("IX_VideoComments_UserId");

        builder.HasIndex(vc => vc.ParentCommentId)
            .HasDatabaseName("IX_VideoComments_ParentCommentId");

        builder.HasIndex(vc => vc.CreatedAt)
            .HasDatabaseName("IX_VideoComments_CreatedAt");

        // Relationships
        builder.HasOne(vc => vc.Video)
            .WithMany(v => v.VideoComments)
            .HasForeignKey(vc => vc.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vc => vc.User)
            .WithMany(u => u.VideoComments)
            .HasForeignKey(vc => vc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing relationship for replies
        builder.HasOne(vc => vc.ParentComment)
            .WithMany(vc => vc.Replies)
            .HasForeignKey(vc => vc.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
