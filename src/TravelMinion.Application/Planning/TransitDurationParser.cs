using System.Globalization;
using System.Text.RegularExpressions;

namespace TravelMinion.Application;

/// <summary>
/// Parses a free-text transit description into minutes. Ported from the Python
/// prototype's <c>_parse_transit_duration</c>.
/// </summary>
internal static partial class TransitDurationParser
{
    public static int? ToMinutes(string? transit)
    {
        if (string.IsNullOrWhiteSpace(transit))
        {
            return null;
        }

        var text = transit.Trim().ToLowerInvariant();
        var total = 0;

        var hours = HoursPattern().Match(text);
        if (hours.Success
            && double.TryParse(hours.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var hourValue))
        {
            total += (int)(hourValue * 60);
        }

        var minutes = MinutesPattern().Match(text);
        if (minutes.Success && int.TryParse(minutes.Groups[1].Value, CultureInfo.InvariantCulture, out var minuteValue))
        {
            total += minuteValue;
        }

        return total > 0 ? total : null;
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(?:h|hours?)")]
    private static partial Regex HoursPattern();

    [GeneratedRegex(@"(\d+)\s*(?:m|mins?|minutes?)")]
    private static partial Regex MinutesPattern();
}
