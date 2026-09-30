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

    public ResearchService(ResearchEngine engine, Func<DateTimeOffset>? clock = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Runs research for every destination in the brief, driving the supplied
    /// job from Queued through Running to Succeeded, or to Failed/Cancelled.
    /// </summary>
    public async Task<ResearchRunResult> RunAsync(
        ResearchJob job,
        TripBrief brief,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(brief);

        job.Start(_clock());

        var suggestions = new List<Suggestion>();
        try
        {
            foreach (var stop in brief.Destinations)
            {
                var found = await _engine.ResearchDestinationAsync(
                    stop.Destination,
                    brief.Interests,
                    stop.Days,
                    brief.PreferredSources,
                    cancellationToken).ConfigureAwait(false);

                job.RecordProgress(stop.Destination, found.Count);
                suggestions.AddRange(found);
            }
        }
        catch (OperationCanceledException)
        {
            job.Cancel(_clock());
            throw;
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message, _clock());
            throw;
        }

        job.Succeed(_clock());
        return new ResearchRunResult(job, suggestions);
    }
}
