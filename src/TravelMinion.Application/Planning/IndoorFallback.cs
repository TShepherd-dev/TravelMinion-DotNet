using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Suggests an indoor alternative for a weather-exposed activity. Ported from
/// the Python prototype's <c>_get_indoor_fallback</c>.
/// </summary>
internal static class IndoorFallback
{
    private static readonly string[] IndoorKeywords =
    {
        "museum", "gallery", "indoor", "theater", "cinema", "shopping mall",
        "aquarium", "planetarium", "library", "archive", "cathedral", "church",
        "temple", "shrine", "palace", "castle", "tower", "observation deck",
    };

    private static readonly (string Keyword, string Fallback)[] Fallbacks =
    {
        ("beach", "Visit nearby aquarium or coastal museum"),
        ("hiking", "Explore local visitor center or nature museum"),
        ("garden", "Tour botanical conservatory or greenhouse"),
        ("park", "Visit nearby museum or indoor market"),
        ("outdoor", "Find covered market or nearby gallery"),
        ("zoo", "Indoor exhibit hall or nearby science museum"),
        ("boat", "Aquarium or maritime museum"),
        ("waterfront", "Indoor food market or shopping arcade"),
        ("viewpoint", "Observation deck in tall building"),
        ("market", "Covered shopping arcade or food hall"),
    };

    private static readonly string[] OutdoorKeywords =
    {
        "beach", "hiking", "garden", "park", "outdoor", "viewpoint", "terrace",
        "rooftop", "boat", "cruise", "ferry", "waterfront", "pier", "boardwalk",
        "zoo", "safari", "botanical", "lake", "river", "mountain", "valley",
        "cliff", "coast", "bay", "harbor", "outdoor market", "open-air",
    };

    public static string? Suggest(ApprovedActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        var text = $"{activity.Name} {activity.Area} {activity.Notes}".ToLowerInvariant();

        foreach (var keyword in IndoorKeywords)
        {
            if (text.Contains(keyword, StringComparison.Ordinal))
            {
                return null;
            }
        }

        foreach (var (keyword, fallback) in Fallbacks)
        {
            if (text.Contains(keyword, StringComparison.Ordinal))
            {
                return fallback;
            }
        }

        foreach (var keyword in OutdoorKeywords)
        {
            if (text.Contains(keyword, StringComparison.Ordinal))
            {
                return "Find nearby indoor attraction or cafe";
            }
        }

        return null;
    }
}
