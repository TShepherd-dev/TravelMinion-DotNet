using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests.Trips;

internal sealed class FakeTripRepository : ITripRepository
{
    private readonly Dictionary<Guid, Trip> _trips = new();

    public int SaveCount { get; private set; }

    public Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_trips.TryGetValue(id, out var trip) ? trip : null);

    public Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Trip>>(
            _trips.Values.OrderBy(trip => trip.Name, StringComparer.Ordinal).ToList());

    public Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trip);
        _trips[trip.Id] = trip;
        return Task.CompletedTask;
    }

    public void Update(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);
        _trips[trip.Id] = trip;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
