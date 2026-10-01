using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Production <see cref="IResearchRunner"/>: queues a Research Job on the Trip,
/// runs the Research Step, merges the produced Suggestions into the Trip
/// (appending by default, so existing Suggestions and approvals survive), and saves.
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

        var merge = ResearchMerge.Apply(trip, result.Suggestions, _mergeMode);

        _repository.Update(trip);

        // A cancelled run still flushes what it found, so its save must not
        // observe the cancelled token.
        var saveToken = result.Job.Status == ResearchJobStatus.Cancelled
            ? CancellationToken.None
            : cancellationToken;

        await _repository.SaveChangesAsync(saveToken).ConfigureAwait(false);

        return result with { Merged = merge };
    }
}
