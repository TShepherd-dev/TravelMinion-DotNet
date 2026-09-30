using Microsoft.EntityFrameworkCore;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence;

internal sealed class TripRepository : ITripRepository
{
    private readonly TravelMinionDbContext _context;

    public TripRepository(TravelMinionDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Trips
            .Include(t => t.ResearchJobs)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
        => await _context.Trips
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        await _context.Trips.AddAsync(trip, cancellationToken).ConfigureAwait(false);
    }

    public void Update(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);

        // ResearchJob carries a client-generated Guid key, so EF cannot tell a newly
        // queued job from one already persisted. Snapshot the jobs EF is already
        // tracking with change detection paused (otherwise merely reading the trip
        // entry auto-attaches the new job as Modified); anything in the aggregate
        // outside that snapshot is new and must be inserted.
        var autoDetect = _context.ChangeTracker.AutoDetectChangesEnabled;
        _context.ChangeTracker.AutoDetectChangesEnabled = false;
        var persistedJobIds = _context.ChangeTracker.Entries<ResearchJob>()
            .Select(entry => entry.Entity.Id)
            .ToHashSet();
        _context.ChangeTracker.AutoDetectChangesEnabled = autoDetect;

        var entry = _context.Entry(trip);
        if (entry.State == EntityState.Detached)
        {
            _context.Attach(trip);
        }

        entry.State = EntityState.Modified;

        foreach (var job in trip.ResearchJobs)
        {
            if (!persistedJobIds.Contains(job.Id))
            {
                _context.Entry(job).State = EntityState.Added;
            }
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
