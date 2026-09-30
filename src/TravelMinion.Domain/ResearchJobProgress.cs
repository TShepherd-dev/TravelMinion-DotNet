namespace TravelMinion.Domain;

/// <summary>
/// Per-destination progress within a Running Research Job.
/// </summary>
public sealed class ResearchJobProgress
{
    internal ResearchJobProgress(string destination) => Destination = destination;

    internal ResearchJobProgress(string destination, int suggestionsFound, bool completed)
    {
        Destination = destination;
        SuggestionsFound = suggestionsFound;
        Completed = completed;
    }

    public string Destination { get; }

    public int SuggestionsFound { get; internal set; }

    public bool Completed { get; internal set; }
}
