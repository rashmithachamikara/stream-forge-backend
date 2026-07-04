using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

public sealed class SystemSecretConfiguration : IEntityTypeConfiguration<SystemSecret>
{
    public void Configure(EntityTypeBuilder<SystemSecret> builder)
    {
        builder.ToTable("SystemSecrets");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(setting => setting.EncryptedValue)
            .IsRequired();

        builder.Property(setting => setting.UpdatedAt)
            .IsRequired();

        builder.HasIndex(setting => setting.Key)
            .IsUnique()
            .HasDatabaseName("IX_SystemSecrets_Key");
    }
}
