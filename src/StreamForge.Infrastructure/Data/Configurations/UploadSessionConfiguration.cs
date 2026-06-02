using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for UploadSession
/// </summary>
public class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.ToTable("UploadSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        builder.Property(s => s.TotalSize)
            .IsRequired();

        builder.Property(s => s.UploadedSize)
            .IsRequired();

        builder.Property(s => s.StorageProviderType)
            .IsRequired();

        builder.Property(s => s.VideoId)
            .IsRequired();

        builder.Property(s => s.TemporaryStoragePath)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(s => s.ContentType)
            .HasMaxLength(100);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        builder.Property(s => s.ExpiresAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.UserId)
            .HasDatabaseName("IX_UploadSessions_UserId");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_UploadSessions_Status");

        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("IX_UploadSessions_ExpiresAt");

        builder.HasIndex(s => s.VideoId)
            .HasDatabaseName("IX_UploadSessions_VideoId");

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Video)
            .WithMany()
            .HasForeignKey(s => s.VideoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<UploadSessionPart>()
            .WithOne()
            .HasForeignKey(p => p.UploadSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
