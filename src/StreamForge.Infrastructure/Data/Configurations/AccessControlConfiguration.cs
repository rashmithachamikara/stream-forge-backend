using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AccessControl
/// </summary>
public class AccessControlConfiguration : IEntityTypeConfiguration<AccessControl>
{
    public void Configure(EntityTypeBuilder<AccessControl> builder)
    {
        builder.ToTable("AccessControls");

        builder.HasKey(ac => ac.Id);

        builder.Property(ac => ac.PermissionType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(ac => ac.ShareToken)
            .HasMaxLength(500);

        builder.Property(ac => ac.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ac => ac.VideoId)
            .HasDatabaseName("IX_AccessControls_VideoId");

        builder.HasIndex(ac => ac.UserId)
            .HasDatabaseName("IX_AccessControls_UserId");

        builder.HasIndex(ac => ac.ShareToken)
            .IsUnique()
            .HasFilter("\"ShareToken\" IS NOT NULL")
            .HasDatabaseName("IX_AccessControls_ShareToken");

        builder.HasIndex(ac => new { ac.VideoId, ac.UserId })
            .HasDatabaseName("IX_AccessControls_VideoId_UserId");

        // Relationships
        builder.HasOne(ac => ac.Video)
            .WithMany(v => v.AccessControls)
            .HasForeignKey(ac => ac.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ac => ac.User)
            .WithMany()
            .HasForeignKey(ac => ac.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
