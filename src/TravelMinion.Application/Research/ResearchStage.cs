namespace TravelMinion.Application;

/// <summary>
/// The stage a Research Job is currently in. Used to render Research Progress
/// while a run is in flight.
/// </summary>
public enum ResearchStage
{
    /// <summary>No run is in flight.</summary>
    Idle,

    /// <summary>Querying the search sources for a destination.</summary>
    SearchingSources,

    /// <summary>Fetching and reading the full pages behind the search hits.</summary>
    ReadingPages,

    /// <summary>Asking the model to extract activities from the pages.</summary>
    ExtractingActivities,

    /// <summary>The run finished successfully.</summary>
    Completed,

    /// <summary>The run was cancelled by the traveller.</summary>
    Cancelled,

    /// <summary>The run failed.</summary>
    Failed,
}
