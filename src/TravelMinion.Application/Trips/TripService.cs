using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Trip use-cases: create a Trip from a Trip Brief, list/load Trips, and curate
/// the Approved Activity List from the Trip's Suggestions.
/// </summary>
public sealed class TripService
{
    private readonly ITripRepository _repository;

    public TripService(ITripRepository repository)
        => _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task<IReadOnlyList<Trip>> ListAsync(CancellationToken cancellationToken = default)
        => _repository.ListAsync(cancellationToken);

    public Task<Trip?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetAsync(id, cancellationToken);

    public async Task<Trip> CreateAsync(
        string name,
        TripBrief brief,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(brief);

        var trip = new Trip(Guid.NewGuid(), name);
        trip.CaptureBrief(brief);

        await _repository.AddAsync(trip, cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return trip;
    }

    public async Task<Trip> RenameAsync(
        Guid tripId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var trip = await LoadAsync(tripId, cancellationToken).ConfigureAwait(false);
        trip.Rename(name);
        _repository.Update(trip);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return trip;
    }

    /// <summary>Deletes a Trip and everything it owns.</summary>
    public async Task DeleteAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await LoadAsync(tripId, cancellationToken).ConfigureAwait(false);
        _repository.Remove(trip);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the Approved Activity List from the selected Suggestions. Indices are
    /// positions in <see cref="Trip.Suggestions"/>; those in <paramref name="mustDoIndices"/>
    /// are marked must-do, the rest become optional fillers.
    /// </summary>
    public async Task<Trip> ApproveActivitiesAsync(
        Guid tripId,
        IReadOnlyCollection<int> approvedIndices,
        IReadOnlyCollection<int> mustDoIndices,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(approvedIndices);
        ArgumentNullException.ThrowIfNull(mustDoIndices);

        var trip = await LoadAsync(tripId, cancellationToken).ConfigureAwait(false);
        var suggestions = trip.Suggestions;

        var approved = new List<ApprovedActivity>();
        for (var index = 0; index < suggestions.Count; index++)
        {
            if (!approvedIndices.Contains(index))
            {
                continue;
            }

            approved.Add(ApprovedActivity.FromSuggestion(
                suggestions[index],
                optional: !mustDoIndices.Contains(index)));
        }

        trip.SetActivities(new ApprovedActivityList(approved));
        _repository.Update(trip);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return trip;
    }

    /// <summary>
    /// Marks Suggestions as discarded so they no longer appear on the list. The
    /// indices are positions in <see cref="Trip.Suggestions"/>.
    /// </summary>
    public async Task<Trip> DiscardSuggestionsAsync(
        Guid tripId,
        IReadOnlyCollection<int> suggestionIndices,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suggestionIndices);

        var trip = await LoadAsync(tripId, cancellationToken).ConfigureAwait(false);
        trip.DiscardSuggestions(suggestionIndices);
        _repository.Update(trip);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return trip;
    }

    private async Task<Trip> LoadAsync(Guid tripId, CancellationToken cancellationToken)
        => await _repository.GetAsync(tripId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Trip {tripId} was not found.");
}
