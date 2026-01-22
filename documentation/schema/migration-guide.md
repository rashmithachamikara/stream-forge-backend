# Database Migration Guide

## Overview

This guide covers database migration strategies for Stream Forge using Entity Framework Core.

---

## Initial Setup

### 1. Install Required Packages

Add to `StreamForge.Infrastructure.csproj`:

```bash
dotnet add src/StreamForge.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/StreamForge.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/StreamForge.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/StreamForge.Infrastructure package Microsoft.EntityFrameworkCore.Tools
```

Add to `StreamForge.Api.csproj` (for design-time support):

```bash
dotnet add src/StreamForge.Api package Microsoft.EntityFrameworkCore.Design
```

---

## Migration Commands

### Create Migration

```bash
dotnet ef migrations add InitialCreate \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api \
  --output-dir Persistence/Migrations
```

### Apply Migration

```bash
# Update to latest
dotnet ef database update \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api

# Update to specific migration
dotnet ef database update MigrationName \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

### Remove Last Migration

```bash
dotnet ef migrations remove \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

### Generate SQL Script

```bash
# All migrations
dotnet ef migrations script \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api \
  --output migrations.sql

# Specific range
dotnet ef migrations script FromMigration ToMigration \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api \
  --output migrations.sql
```

### List Migrations

```bash
dotnet ef migrations list \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

---

## Migration Strategy

### Phase 1: Core Schema (Initial Migration)

**Migration Name:** `InitialCreate`

**Tables Included:**
- Users
- Videos (with embedded settings)
- VideoVersions
- VideoFiles
- VideoThumbnails
- VideoProcessingJobs
- StorageProviders
- Categories
- Tags
- VideoTags

**Command:**
```bash
dotnet ef migrations add InitialCreate \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

---

### Phase 2: Engagement Features

**Migration Name:** `AddEngagementFeatures`

**Tables Included:**
- VideoReactions (split from old Feedback)
- VideoComments (split from old Feedback)
- Bookmarks
- Playlists
- PlaylistVideos

**Command:**
```bash
dotnet ef migrations add AddEngagementFeatures \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

---

### Phase 3: Advanced Features

**Migration Name:** `AddAdvancedFeatures`

**Tables Included:**
- VideoTranscriptions
- AccessControl
- Notifications
- AnalyticsEvents

**Command:**
```bash
dotnet ef migrations add AddAdvancedFeatures \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

---

## Seeding Data

### Create Seed Data

Create `src/StreamForge.Infrastructure/Persistence/DataSeeder.cs`:

```csharp
public static class DataSeeder
{
    public static async Task SeedAsync(StreamForgeDbContext context)
    {
        // Seed default storage provider
        if (!await context.StorageProviders.AnyAsync())
        {
            context.StorageProviders.Add(new StorageProvider
            {
                Id = Guid.NewGuid(),
                Name = "Local Storage",
                Type = StorageProviderType.Local,
                Configuration = "{ \"basePath\": \"./storage\" }",
                IsDefault = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Seed default categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                "Education", "Entertainment", "Technology",
                "Business", "Sports", "Music", "Gaming"
            };

            foreach (var cat in categories)
            {
                context.Categories.Add(new Category
                {
                    Id = Guid.NewGuid(),
                    Name = cat,
                    Description = $"{cat} videos",
                    DisplayOrder = Array.IndexOf(categories, cat),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Seed admin user
        if (!await context.Users.AnyAsync())
        {
            context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Name = "System Administrator",
                Email = "admin@streamforge.local",
                PasswordHash = "CHANGE_ME", // Use proper password hashing
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }
}
```

### Apply Seeds

In `Program.cs`:

```csharp
// After app.Build()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<StreamForgeDbContext>();
    await context.Database.MigrateAsync(); // Apply migrations
    await DataSeeder.SeedAsync(context);   // Seed data
}
```

---

## Migration Best Practices

### 1. Always Review Generated Migrations

```bash
# Check the generated migration file before applying
code src/StreamForge.Infrastructure/Persistence/Migrations/XXXXXX_MigrationName.cs
```

### 2. Test Migrations on Development Database First

```bash
# Use separate connection string for testing
export ConnectionStrings__DefaultConnection="Host=localhost;Database=streamforge_test;..."
dotnet ef database update
```

### 3. Backup Production Database Before Migration

```bash
# PostgreSQL backup
pg_dump -U postgres -h localhost streamforge > backup_$(date +%Y%m%d_%H%M%S).sql
```

### 4. Use Idempotent Scripts for Production

```bash
# Generate idempotent SQL script
dotnet ef migrations script --idempotent \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api \
  --output production_migration.sql
```

### 5. Version Control Migration Files

```bash
git add src/StreamForge.Infrastructure/Persistence/Migrations/
git commit -m "Add migration: MigrationName"
```

---

## Rollback Strategy

### Rollback to Previous Migration

```bash
# List migrations to find target
dotnet ef migrations list \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api

# Rollback to specific migration
dotnet ef database update PreviousMigrationName \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

### Complete Rollback (Drop Database)

```bash
dotnet ef database drop \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api \
  --force
```

---

## Handling Schema Changes

### Adding a Column

1. Add property to domain entity
2. Update entity configuration (Fluent API)
3. Create migration
4. Apply migration

```bash
dotnet ef migrations add AddColumnNameToTable \
  --project src/StreamForge.Infrastructure \
  --startup-project src/StreamForge.Api
```

### Renaming a Column

Use `RenameColumn` in migration:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.RenameColumn(
        name: "OldColumnName",
        table: "TableName",
        newName: "NewColumnName");
}
```

### Dropping a Column

**⚠️ WARNING: Data Loss**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "ColumnName",
        table: "TableName");
}
```

### Adding an Index

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateIndex(
        name: "IX_TableName_ColumnName",
        table: "TableName",
        column: "ColumnName");
}
```

---

## Performance Considerations

### Large Table Migrations

For tables with millions of rows:

1. **Avoid adding NOT NULL columns without defaults**
2. **Add indexes online** (if supported)
3. **Use batched operations**
4. **Schedule during maintenance windows**

### Index Creation

```sql
-- PostgreSQL: Create index concurrently (doesn't lock table)
CREATE INDEX CONCURRENTLY IX_Videos_CreatedAt ON "Videos" ("CreatedAt");
```

---

## Production Migration Checklist

- [ ] Test migration on development database
- [ ] Test migration on staging database
- [ ] Review generated SQL script
- [ ] Backup production database
- [ ] Schedule maintenance window (if needed)
- [ ] Apply migration
- [ ] Verify data integrity
- [ ] Monitor application logs
- [ ] Have rollback plan ready

---

## Troubleshooting

### Migration Fails: "Table already exists"

```bash
# Reset migrations
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Migration Fails: "Column does not exist"

Check for:
1. Uncommitted migrations in source control
2. Manual database changes
3. Out-of-sync migration history

### Design-Time DbContext Error

Ensure `Program.cs` or `DesignTimeDbContextFactory` is configured:

```csharp
// src/StreamForge.Infrastructure/Persistence/DesignTimeDbContextFactory.cs
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<StreamForgeDbContext>
{
    public StreamForgeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<StreamForgeDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=streamforge;Username=postgres;Password=postgres");
        return new StreamForgeDbContext(optionsBuilder.Options);
    }
}
```

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Database Migrations

on:
  push:
    branches: [main]

jobs:
  migrate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      
      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef
      
      - name: Generate migration script
        run: |
          dotnet ef migrations script --idempotent \
            --project src/StreamForge.Infrastructure \
            --startup-project src/StreamForge.Api \
            --output migrations.sql
      
      - name: Upload migration script
        uses: actions/upload-artifact@v2
        with:
          name: migration-script
          path: migrations.sql
```

---

## References

- [EF Core Migrations Documentation](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL Migration Best Practices](https://www.postgresql.org/docs/current/ddl-alter.html)
- [Schema Versioning Strategy](https://martinfowler.com/articles/evodb.html)

---

**Last Updated:** January 22, 2026  
**Schema Version:** 1.0
