using System.Globalization;
using System.Text.RegularExpressions;

namespace TravelMinion.Application;

/// <summary>
/// Parses a free-text activity duration into minutes. Ported from the Python
/// prototype's <c>_parse_duration</c>.
/// </summary>
internal static partial class DurationParser
{
    public static int ToMinutes(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return PlannerConstants.DefaultActivityDuration;
        }

        var text = duration.Trim().ToLowerInvariant();

        var range = RangePattern().Match(text);
        if (range.Success)
        {
            var low = int.Parse(range.Groups[1].Value, CultureInfo.InvariantCulture);
            var high = int.Parse(range.Groups[2].Value, CultureInfo.InvariantCulture);
            return (int)((low + high) / 2.0 * 60);
        }

        var hours = HoursPattern().Match(text);
        if (hours.Success
            && double.TryParse(hours.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hourValue))
        {
            return (int)(hourValue * 60);
        }

        var minutes = MinutesPattern().Match(text);
        if (minutes.Success && int.TryParse(minutes.Groups[1].Value, CultureInfo.InvariantCulture, out var minuteValue))
        {
            return minuteValue;
        }

        if (text.Contains("half", StringComparison.Ordinal))
        {
            return 240;
        }

        if (text.Contains("full", StringComparison.Ordinal))
        {
            return 480;
        }

        return PlannerConstants.DefaultActivityDuration;
    }

    [GeneratedRegex(@"(\d+)\s*-\s*(\d+)\s*hours?")]
    private static partial Regex RangePattern();

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*hours?")]
    private static partial Regex HoursPattern();

    [GeneratedRegex(@"(\d+)\s*min")]
    private static partial Regex MinutesPattern();
}
