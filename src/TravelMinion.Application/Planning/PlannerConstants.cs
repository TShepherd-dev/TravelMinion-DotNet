namespace TravelMinion.Application;

/// <summary>
/// Time constants and thresholds used by the itinerary planner, all in minutes
/// since midnight unless noted otherwise.
/// </summary>
internal static class PlannerConstants
{
    public const int MorningStart = 9 * 60;

    public const int EveningEnd = 18 * 60;

    public const int LunchStart = 12 * 60;

    public const int LunchEnd = 13 * 60;

    public const int DinnerStart = 17 * 60;

    public const int DefaultActivityDuration = 120;

    public const int DefaultTransit = 30;

    /// <summary>Travel legs at or over this many minutes become recovery Free Days.</summary>
    public const int LongHaulThreshold = 360;
}
