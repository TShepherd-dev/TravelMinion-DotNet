using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>The outcome of running the Research Step for a Trip.</summary>
/// <param name="Job">The Research Job that was advanced.</param>
/// <param name="Suggestions">The Suggestions produced across all destinations.</param>
/// <param name="Merged">
/// How the produced Suggestions were merged into the Trip. Null until
/// <see cref="ResearchRunner"/> has merged them.
/// </param>
public sealed record ResearchRunResult(
    ResearchJob Job,
    IReadOnlyList<Suggestion> Suggestions,
    ResearchMergeResult? Merged = null);

/// <summary>
/// Runs the Research Step for a Trip Brief, advancing a Research Job through its
/// lifecycle and recording per-destination progress. This is the use-case the
/// queue worker will invoke; keeping it here makes the pipeline testable without
/// a host.
/// </summary>
public sealed class ResearchService
{
    private readonly ResearchEngine _engine;
    private readonly Func<DateTimeOffset> _clock;
    private readonly ResearchProgressReporter? _progress;

    public ResearchService(
        ResearchEngine engine,
        Func<DateTimeOffset>? clock = null,
        ResearchProgressReporter? progress = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _progress = progress;
    }

    /// <summary>
    /// Runs research for every base in the brief, driving the supplied
    /// job from Queued through Running to Succeeded, or to Failed/Cancelled.
    /// A cancelled run returns the Suggestions found so far rather than throwing,
    /// so the caller can keep them; a failed run throws and keeps nothing.
    /// </summary>
    public async Task<ResearchRunResult> RunAsync(
        ResearchJob job,
        TripBrief brief,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(brief);

        job.Start(_clock());
        _progress?.Begin(brief.Bases.Count);

        var suggestions = new List<Suggestion>();
        try
        {
            for (var index = 0; index < brief.Bases.Count; index++)
            {
                var @base = brief.Bases[index];
                _progress?.SetDestination(@base.Name, index + 1);

                var found = await _engine.ResearchDestinationAsync(
                    @base.Name,
                    brief.Interests,
                    @base.Days,
                    brief.PreferredSources,
                    cancellationToken).ConfigureAwait(false);

                job.RecordProgress(@base.Name, found.Count);
                suggestions.AddRange(found);
                _progress?.AddSuggestions(found);
            }
        }
        catch (OperationCanceledException)
        {
            job.Cancel(_clock());
            _progress?.Finish(ResearchStage.Cancelled);
            return new ResearchRunResult(job, suggestions);
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message, _clock());
            _progress?.Finish(ResearchStage.Failed);
            throw;
        }

        job.Succeed(_clock());
        _progress?.Finish(ResearchStage.Completed);
        return new ResearchRunResult(job, suggestions);
    }
}
