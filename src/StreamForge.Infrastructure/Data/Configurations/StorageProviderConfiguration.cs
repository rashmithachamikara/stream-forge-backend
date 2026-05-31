using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for StorageProvider
/// </summary>
public class StorageProviderConfiguration : IEntityTypeConfiguration<StorageProvider>
{
    public void Configure(EntityTypeBuilder<StorageProvider> builder)
    {
        builder.ToTable("StorageProviders");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sp => sp.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(sp => sp.Configuration)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(sp => sp.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sp => sp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sp => sp.CreatedAt)
            .IsRequired();

        builder.Property(sp => sp.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(sp => sp.Type)
            .HasDatabaseName("IX_StorageProviders_Type");

        builder.HasIndex(sp => sp.IsDefault)
            .HasDatabaseName("IX_StorageProviders_IsDefault");

        builder.HasIndex(sp => sp.IsActive)
            .HasDatabaseName("IX_StorageProviders_IsActive");

        // Relationships
        builder.HasMany(sp => sp.VideoFiles)
            .WithOne(vf => vf.StorageProvider)
            .HasForeignKey(vf => vf.StorageProviderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
