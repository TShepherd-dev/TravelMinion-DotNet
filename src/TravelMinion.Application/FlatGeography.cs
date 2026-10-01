using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Transitional mapping from a flat list of destinations to the Country -> Base
/// geography, treating each destination as a single-base country. Used by the
/// interview and the quick-create form until the extractor and builder emit
/// nested geography.
/// </summary>
public static class FlatGeography
{
    public static IReadOnlyList<Country> ToCountries(IEnumerable<(string Name, int Days)> destinations)
    {
        ArgumentNullException.ThrowIfNull(destinations);

        return destinations
            .Select(destination =>
            {
                var days = Math.Max(1, destination.Days);
                return new Country(destination.Name, days, new[] { new Base(destination.Name, days) });
            })
            .ToList();
    }
}
