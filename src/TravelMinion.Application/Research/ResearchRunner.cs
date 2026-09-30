using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Production <see cref="IResearchRunner"/>: queues a Research Job on the Trip,
/// runs the Research Step, replaces the Trip's Suggestions, and saves.
/// </summary>
public sealed class ResearchRunner : IResearchRunner
{
    private readonly ResearchService _researchService;
    private readonly ITripRepository _repository;
    private readonly ResearchMergeMode _mergeMode;
    private readonly Func<DateTimeOffset> _clock;

    public ResearchRunner(
        ResearchService researchService,
        ITripRepository repository,
        ResearchMergeMode mergeMode = ResearchMergeMode.Append,
        Func<DateTimeOffset>? clock = null)
    {
        _researchService = researchService ?? throw new ArgumentNullException(nameof(researchService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mergeMode = mergeMode;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public bool IsAvailable => true;

    public async Task<ResearchRunResult> RunAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var brief = trip.Brief
            ?? throw new InvalidOperationException("The Trip has no Trip Brief to research.");

        var job = trip.QueueResearch(_clock());
        var result = await _researchService.RunAsync(job, brief, cancellationToken).ConfigureAwait(false);

        var merge = ResearchMerge.Combine(trip, result.Suggestions, _mergeMode);
        if (_mergeMode == ResearchMergeMode.Replace)
        {
            trip.ReplaceSuggestions(merge.Suggestions);
        }
        else
        {
            trip.AppendSuggestions(merge.Suggestions.Skip(trip.Suggestions.Count));
        }

        _repository.Update(trip);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return result with { Merged = merge };
    }
}
