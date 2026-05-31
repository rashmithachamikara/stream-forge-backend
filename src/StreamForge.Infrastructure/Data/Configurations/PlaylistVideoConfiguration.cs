using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for PlaylistVideo (join table)
/// </summary>
public class PlaylistVideoConfiguration : IEntityTypeConfiguration<PlaylistVideo>
{
    public void Configure(EntityTypeBuilder<PlaylistVideo> builder)
    {
        builder.ToTable("PlaylistVideos");

        builder.HasKey(pv => new { pv.PlaylistId, pv.VideoId });

        builder.Property(pv => pv.OrderIndex)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pv => pv.AddedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(pv => new { pv.PlaylistId, pv.VideoId })
            .IsUnique()
            .HasDatabaseName("IX_PlaylistVideos_PlaylistId_VideoId");

        builder.HasIndex(pv => pv.VideoId)
            .HasDatabaseName("IX_PlaylistVideos_VideoId");

        builder.HasIndex(pv => new { pv.PlaylistId, pv.OrderIndex })
            .HasDatabaseName("IX_PlaylistVideos_PlaylistId_OrderIndex");

        // Relationships
        builder.HasOne(pv => pv.Playlist)
            .WithMany(p => p.PlaylistVideos)
            .HasForeignKey(pv => pv.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pv => pv.Video)
            .WithMany(v => v.PlaylistVideos)
            .HasForeignKey(pv => pv.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
