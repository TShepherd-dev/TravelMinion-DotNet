using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Persistence port for the <see cref="Trip"/> aggregate. Implementations load and
/// store the whole aggregate; callers mutate the returned instance and then call
/// <see cref="SaveChangesAsync"/>.
/// </summary>
public interface ITripRepository
{
    /// <summary>Loads a Trip with its Brief, Suggestions, Activities and Research Jobs.</summary>
    Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lists all Trips (without their child graphs).</summary>
    Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Stages a new Trip for insertion.</summary>
    Task AddAsync(Trip trip, CancellationToken cancellationToken = default);

    /// <summary>Stages an existing Trip (and its graph) for update.</summary>
    void Update(Trip trip);

    /// <summary>Stages an existing Trip (and its graph) for deletion.</summary>
    void Remove(Trip trip);

    /// <summary>Persists staged changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
