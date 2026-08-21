using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreamForge.Application.Common;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Authentication;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(
        StreamForgeDbContext context,
        ILogger? logger = null,
        SeedAdminOptions? seedAdminOptions = null)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var seededItems = new List<string>();

        // Storage provider
        if (!await context.StorageProviders.AnyAsync())
        {
            var config = JsonSerializer.Serialize(new { basePath = "./uploads" });
            var provider = StorageProvider.Create("Local Storage", StorageProviderType.Local, config, isDefault: true);
            context.StorageProviders.Add(provider);
            seededItems.Add("Local Storage provider");
        }

        // Basic categories (if not present)
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[] { "Education", "Entertainment", "Technology", "Business", "Sports" };
            foreach (var (name, idx) in categories.Select((n, i) => (n, i)))
            {
                var category = Category.Create(name, description: name + " videos", parentCategoryId: null, displayOrder: idx);
                context.Categories.Add(category);
            }

            seededItems.Add("default categories");
        }

        if (seedAdminOptions is not null && !string.IsNullOrWhiteSpace(seedAdminOptions.Password))
        {
            if (seedAdminOptions.Password.Length < 8)
            {
                throw new InvalidOperationException("The seeded administrator password must be at least 8 characters long.");
            }

            if (string.IsNullOrWhiteSpace(seedAdminOptions.Name) || string.IsNullOrWhiteSpace(seedAdminOptions.Email))
            {
                throw new InvalidOperationException("The seeded administrator name and email must not be empty.");
            }

            var adminEmail = seedAdminOptions.Email.Trim().ToLowerInvariant();
            var existingAdmin = await context.Users.SingleOrDefaultAsync(user => user.Email == adminEmail);

            if (existingAdmin is null)
            {
                var admin = User.Create(
                    seedAdminOptions.Name.Trim(),
                    adminEmail,
                    PasswordHasher.Hash(seedAdminOptions.Password),
                    UserRole.Admin);
                context.Users.Add(admin);
                seededItems.Add("administrator account");
            }
        }
        else if (!await context.Users.AnyAsync())
        {
            logger?.LogWarning(
                "No administrator was seeded because SeedAdmin:Password is empty. Configure STREAMFORGE_SEED_ADMIN_PASSWORD before first startup.");
        }

        if (seededItems.Count == 0)
        {
            logger?.LogInformation("Nothing to seed.");
            return;
        }

        await context.SaveChangesAsync();
        logger?.LogInformation("Seeded: {SeededItems}", string.Join(", ", seededItems));
    }
}
