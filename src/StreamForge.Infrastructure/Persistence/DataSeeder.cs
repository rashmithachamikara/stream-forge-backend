using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;
using StreamForge.Infrastructure.Data;

namespace StreamForge.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(StreamForgeDbContext context, ILogger? logger = null)
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

        // Admin user placeholder (skip password hashing here)
        if (!await context.Users.AnyAsync())
        {
            // Use the factory method to create the admin user
            var admin = User.Create("Administrator", "admin@streamforge.local", "CHANGE_ME", UserRole.Admin);
            context.Users.Add(admin);
            seededItems.Add("admin user placeholder");
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
