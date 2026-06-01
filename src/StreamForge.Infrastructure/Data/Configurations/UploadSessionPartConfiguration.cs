using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for UploadSessionPart
/// </summary>
public class UploadSessionPartConfiguration : IEntityTypeConfiguration<UploadSessionPart>
{
    public void Configure(EntityTypeBuilder<UploadSessionPart> builder)
    {
        builder.ToTable("UploadSessionParts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UploadSessionId)
            .IsRequired();

        builder.Property(p => p.PartNumber)
            .IsRequired();

        builder.Property(p => p.Size)
            .IsRequired();

        builder.Property(p => p.Checksum)
            .IsRequired()
            .HasMaxLength(256); // SHA256 hash in hex = 64 chars, MD5 = 32, allow space

        builder.Property(p => p.StoragePath)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.IsComplete)
            .IsRequired();

        builder.Property(p => p.UploadedAt)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.UploadSessionId)
            .HasDatabaseName("IX_UploadSessionParts_UploadSessionId");

        builder.HasIndex(p => new { p.UploadSessionId, p.PartNumber })
            .IsUnique()
            .HasDatabaseName("IX_UploadSessionParts_SessionId_PartNumber");

        builder.HasIndex(p => p.IsComplete)
            .HasDatabaseName("IX_UploadSessionParts_IsComplete");

        // Relationships
        builder.HasOne<UploadSession>()
            .WithMany()
            .HasForeignKey(p => p.UploadSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
