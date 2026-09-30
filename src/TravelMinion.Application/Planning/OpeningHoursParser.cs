using System.Globalization;
using System.Text.RegularExpressions;

namespace TravelMinion.Application;

/// <summary>
/// Parses a free-text opening-hours string into an open/close pair. Ported from
/// the Python prototype's <c>_parse_opening_hours</c>. Returns null for
/// unparseable or always-open values.
/// </summary>
internal static partial class OpeningHoursParser
{
    public static (TimeOnly Open, TimeOnly Close)? Parse(string? hours)
    {
        if (string.IsNullOrWhiteSpace(hours))
        {
            return null;
        }

        var text = hours.Trim().ToLowerInvariant();
        if (text.Contains("24", StringComparison.Ordinal) || text.Contains("always", StringComparison.Ordinal))
        {
            return null;
        }

        var match = Pattern().Match(text);
        if (!match.Success)
        {
            return null;
        }

        var startHour = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var startMinute = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        var startMeridiem = match.Groups[3].Value;
        var endHour = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);
        var endMinute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : 0;
        var endMeridiem = match.Groups[6].Value;

        if (startMeridiem == "pm" && startHour != 12)
        {
            startHour += 12;
        }

        if (endMeridiem == "pm" && endHour != 12)
        {
            endHour += 12;
        }

        if (startHour > 23 || endHour > 23)
        {
            return null;
        }

        return (new TimeOnly(startHour, startMinute), new TimeOnly(endHour, endMinute));
    }

    [GeneratedRegex(@"^(\d{1,2})(?::(\d{2}))?\s*(am|pm)?\s*-\s*(\d{1,2})(?::(\d{2}))?\s*(am|pm)?")]
    private static partial Regex Pattern();
}
