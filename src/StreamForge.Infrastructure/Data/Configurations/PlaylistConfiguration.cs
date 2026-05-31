using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Playlist
/// </summary>
public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
{
    public void Configure(EntityTypeBuilder<Playlist> builder)
    {
        builder.ToTable("Playlists");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OwnerId)
            .IsRequired();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(5000);

        builder.Property(p => p.Visibility)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.VideoCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.OwnerId)
            .HasDatabaseName("IX_Playlists_OwnerId");

        builder.HasIndex(p => p.Visibility)
            .HasDatabaseName("IX_Playlists_Visibility");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Playlists_CreatedAt");

        // Relationships
        builder.HasOne(p => p.Owner)
            .WithMany(u => u.Playlists)
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PlaylistVideos)
            .WithOne(pv => pv.Playlist)
            .HasForeignKey(pv => pv.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
