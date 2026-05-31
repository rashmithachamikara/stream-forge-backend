using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Bookmark
/// </summary>
public class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(b => new { b.UserId, b.VideoId })
            .IsUnique()
            .HasDatabaseName("IX_Bookmarks_UserId_VideoId");

        builder.HasIndex(b => b.VideoId)
            .HasDatabaseName("IX_Bookmarks_VideoId");

        // Relationships
        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookmarks)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Video)
            .WithMany(v => v.Bookmarks)
            .HasForeignKey(b => b.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
