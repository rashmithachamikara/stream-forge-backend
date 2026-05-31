using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for VideoFile
/// </summary>
public class VideoFileConfiguration : IEntityTypeConfiguration<VideoFile>
{
    public void Configure(EntityTypeBuilder<VideoFile> builder)
    {
        builder.ToTable("VideoFiles");

        builder.HasKey(vf => vf.Id);

        builder.Property(vf => vf.VideoVersionId)
            .IsRequired();

        builder.Property(vf => vf.StorageProviderId)
            .IsRequired();

        builder.Property(vf => vf.FilePath)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(vf => vf.FileSize)
            .IsRequired();

        builder.Property(vf => vf.MimeType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(vf => vf.Checksum)
            .HasMaxLength(64);

        builder.Property(vf => vf.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(vf => vf.VideoVersionId)
            .HasDatabaseName("IX_VideoFiles_VideoVersionId");

        builder.HasIndex(vf => vf.StorageProviderId)
            .HasDatabaseName("IX_VideoFiles_StorageProviderId");

        builder.HasIndex(vf => vf.FilePath)
            .HasDatabaseName("IX_VideoFiles_FilePath");

        builder.HasIndex(vf => vf.Checksum)
            .HasDatabaseName("IX_VideoFiles_Checksum");

        // Relationships
        builder.HasOne(vf => vf.VideoVersion)
            .WithMany(vv => vv.Files)
            .HasForeignKey(vf => vf.VideoVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vf => vf.StorageProvider)
            .WithMany(sp => sp.VideoFiles)
            .HasForeignKey(vf => vf.StorageProviderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
