using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StreamForge.Domain.Entities;

namespace StreamForge.Infrastructure.Data.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(setting => setting.Value)
            .IsRequired();

        builder.Property(setting => setting.UpdatedAt)
            .IsRequired();

        builder.HasIndex(setting => setting.Key)
            .IsUnique()
            .HasDatabaseName("IX_SystemSettings_Key");
    }
}
