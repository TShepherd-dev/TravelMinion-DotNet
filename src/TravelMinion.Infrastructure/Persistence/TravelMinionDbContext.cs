using Microsoft.EntityFrameworkCore;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence;

/// <summary>EF Core context for the TravelMinion aggregate.</summary>
public sealed class TravelMinionDbContext : DbContext
{
    public TravelMinionDbContext(DbContextOptions<TravelMinionDbContext> options)
        : base(options)
    {
    }

    public DbSet<Trip> Trips => Set<Trip>();

    public DbSet<ResearchJob> ResearchJobs => Set<ResearchJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TravelMinionDbContext).Assembly);
    }
}
