namespace TravelMinion.Domain;

/// <summary>
/// Invariant 3 helper: splits a total number of days across a number of items as
/// evenly as possible, with the remainder going to the earlier items.
/// </summary>
internal static class DayDistribution
{
    public static IReadOnlyList<int> Distribute(int totalDays, int itemCount)
    {
        var days = new int[itemCount];
        var each = totalDays / itemCount;
        var remainder = totalDays % itemCount;

        for (var i = 0; i < itemCount; i++)
        {
            days[i] = each + (i < remainder ? 1 : 0);
        }

        return days;
    }
}
