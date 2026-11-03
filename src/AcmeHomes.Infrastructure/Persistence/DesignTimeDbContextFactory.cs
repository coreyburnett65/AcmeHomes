using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AcmeHomes.Infrastructure.Persistence;

/// <summary>
/// Lets the dotnet-ef tool create the context without a startup project. Adding a migration doesn't connect to a
/// database, so the connection string is a placeholder with no credentials. The app applies migrations itself at
/// runtime, using the connection string from Aspire or Azure configuration.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AcmeHomesDbContext>
{
    public AcmeHomesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AcmeHomesDbContext>()
            .UseSqlServer("Server=localhost;Database=AcmeHomes;Integrated Security=true;TrustServerCertificate=true")
            .Options;

        return new AcmeHomesDbContext(options);
    }
}
