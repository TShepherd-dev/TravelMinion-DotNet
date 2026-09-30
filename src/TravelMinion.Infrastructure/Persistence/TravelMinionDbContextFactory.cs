using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelMinion.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> to create migrations. Migrations are
/// generated for SQL Server (the production provider); the SQLite dev/test database is
/// created with <c>EnsureCreated</c> instead of migrations.
/// </summary>
internal sealed class TravelMinionDbContextFactory : IDesignTimeDbContextFactory<TravelMinionDbContext>
{
    public TravelMinionDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TravelMinionDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TravelMinion;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new TravelMinionDbContext(options);
    }
}
