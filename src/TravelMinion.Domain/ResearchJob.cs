namespace TravelMinion.Domain;

/// <summary>
/// A single execution of the Research Step for a Trip. Runs asynchronously and
/// produces the Trip's Suggestions.
/// </summary>
public sealed class ResearchJob
{
    private readonly List<ResearchJobProgress> _progress = new();

    public ResearchJob(Guid id, Guid tripId, DateTimeOffset createdAt)
    {
        Id = id;
        TripId = tripId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid TripId { get; }

    public ResearchJobStatus Status { get; private set; } = ResearchJobStatus.Queued;

    public int AttemptCount { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? FailureReason { get; private set; }

    public IReadOnlyList<ResearchJobProgress> Progress => _progress;

    /// <summary>
    /// Reattaches persisted progress when the Research Job is loaded from storage.
    /// </summary>
    internal void ReplaceProgress(IEnumerable<ResearchJobProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        _progress.Clear();
        _progress.AddRange(progress);
    }

    public static ResearchJob Queue(Guid tripId, DateTimeOffset now, int attempt = 0)
    {
        if (attempt < 0)
        {
            throw new DomainException("A research job's attempt count must not be negative.");
        }

        return new ResearchJob(Guid.NewGuid(), tripId, now) { AttemptCount = attempt };
    }

    public void Start(DateTimeOffset now)
    {
        if (Status != ResearchJobStatus.Queued)
        {
            throw new DomainException($"Cannot start a research job in the {Status} state.");
        }

        Status = ResearchJobStatus.Running;
        StartedAt = now;
    }

    public void RecordProgress(string destination, int suggestionsFound, bool completed = true)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new DomainException("Research job progress requires a destination.");
        }

        var entry = _progress.FirstOrDefault(progress => progress.Destination == destination);
        if (entry is null)
        {
            entry = new ResearchJobProgress(destination.Trim());
            _progress.Add(entry);
        }

        entry.SuggestionsFound = suggestionsFound;
        entry.Completed = completed;
    }

    public void Succeed(DateTimeOffset now)
    {
        if (Status != ResearchJobStatus.Running)
        {
            throw new DomainException($"Cannot complete a research job in the {Status} state.");
        }

        Status = ResearchJobStatus.Succeeded;
        FailureReason = null;
        CompletedAt = now;
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        if (Status is ResearchJobStatus.Succeeded or ResearchJobStatus.Cancelled)
        {
            throw new DomainException($"Cannot fail a research job in the {Status} state.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A failed research job requires a reason.");
        }

        Status = ResearchJobStatus.Failed;
        FailureReason = reason.Trim();
        CompletedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is ResearchJobStatus.Succeeded or ResearchJobStatus.Failed or ResearchJobStatus.Cancelled)
        {
            throw new DomainException($"Cannot cancel a research job in the {Status} state.");
        }

        Status = ResearchJobStatus.Cancelled;
        CompletedAt = now;
    }
}
