using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Holds the Research Progress of the run in flight and raises <see cref="Changed"/>
/// whenever it moves, so the UI can re-render. Registered per circuit, so the
/// progress (and the activities found so far) survives navigating away from and
/// back to the Trip. Never persisted.
/// </summary>
public sealed class ResearchProgressReporter : IResearchProgressSink
{
    private readonly object _gate = new();
    private readonly List<Suggestion> _suggestions = new();
    private ResearchProgressState _current = ResearchProgressState.Idle;
    private CancellationTokenSource? _cancellation;

    /// <summary>The latest snapshot of the run in flight.</summary>
    public ResearchProgressState Current => _current;

    /// <summary>Raised after every change to <see cref="Current"/>.</summary>
    public event Action? Changed;

    /// <summary>
    /// Starts a run and returns a token the caller can cancel. Called by the UI
    /// before handing the token to the runner.
    /// </summary>
    public CancellationToken StartRun()
    {
        lock (_gate)
        {
            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            return _cancellation.Token;
        }
    }

    /// <summary>Cancels the run in flight, if any.</summary>
    public void Cancel()
    {
        lock (_gate)
        {
            _cancellation?.Cancel();
        }
    }

    /// <summary>Resets the progress for a new run across the given destinations.</summary>
    public void Begin(int destinationCount)
    {
        lock (_gate)
        {
            _suggestions.Clear();
            _current = new ResearchProgressState(
                ResearchStage.SearchingSources,
                null,
                0,
                destinationCount,
                Array.Empty<Suggestion>());
        }

        Notify();
    }

    /// <summary>Records which destination is being worked, and its position.</summary>
    public void SetDestination(string destination, int index)
    {
        lock (_gate)
        {
            _current = _current with { Destination = destination, DestinationIndex = index };
        }

        Notify();
    }

    /// <inheritdoc />
    public void Stage(ResearchStage stage)
    {
        lock (_gate)
        {
            _current = _current with { Stage = stage };
        }

        Notify();
    }

    /// <summary>Adds the Suggestions found for a destination to the running total.</summary>
    public void AddSuggestions(IEnumerable<Suggestion> suggestions)
    {
        lock (_gate)
        {
            _suggestions.AddRange(suggestions);
            _current = _current with { Suggestions = _suggestions.ToArray() };
        }

        Notify();
    }

    /// <summary>Marks the run finished in a terminal stage and releases the token.</summary>
    public void Finish(ResearchStage terminalStage)
    {
        lock (_gate)
        {
            _current = _current with { Stage = terminalStage };
            _cancellation?.Dispose();
            _cancellation = null;
        }

        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
