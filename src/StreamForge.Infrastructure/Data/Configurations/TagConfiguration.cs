using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Tag
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("IX_Tags_Name");

        builder.HasIndex(t => t.UsageCount)
            .HasDatabaseName("IX_Tags_UsageCount");

        // Relationships
        builder.HasMany(t => t.VideoTags)
            .WithOne(vt => vt.Tag)
            .HasForeignKey(vt => vt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
