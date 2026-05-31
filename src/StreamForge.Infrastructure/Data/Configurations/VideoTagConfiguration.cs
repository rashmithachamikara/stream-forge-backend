using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoTag (join table)
/// </summary>
public class VideoTagConfiguration : IEntityTypeConfiguration<VideoTag>
{
    public void Configure(EntityTypeBuilder<VideoTag> builder)
    {
        builder.ToTable("VideoTags");

        builder.HasKey(vt => new { vt.VideoId, vt.TagId });

        builder.Property(vt => vt.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vt => vt.TagId)
            .HasDatabaseName("IX_VideoTags_TagId");

        // Relationships
        builder.HasOne(vt => vt.Video)
            .WithMany(v => v.VideoTags)
            .HasForeignKey(vt => vt.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vt => vt.Tag)
            .WithMany(t => t.VideoTags)
            .HasForeignKey(vt => vt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
