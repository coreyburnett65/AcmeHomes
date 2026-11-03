using AcmeHomes.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace AcmeHomes.Infrastructure.Tests;

public class MigrationTests
{
    [Fact]
    public void Model_has_no_changes_missing_from_migrations()
    {
        // Compares the current model with the latest migration snapshot. No database connection is opened.
        var options = new DbContextOptionsBuilder<AcmeHomesDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;
        using var context = new AcmeHomesDbContext(options);

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "The EF Core model has changed since the last migration. Run: dotnet ef migrations add <Name> --project src/AcmeHomes.Infrastructure --output-dir Persistence/Migrations");
    }
}
