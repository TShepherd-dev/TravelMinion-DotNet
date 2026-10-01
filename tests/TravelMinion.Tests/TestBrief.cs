using TravelMinion.Domain;

namespace TravelMinion.Tests;

/// <summary>
/// Test helper that builds a <see cref="TripBrief"/> from a flat chain of bases,
/// wrapping each base in its own single-base country. Keeps the older flat-style
/// tests readable while the geography is nested.
/// </summary>
internal static class TestBrief
{
    public static TripBrief FromBases(
        IEnumerable<Base> bases,
        DateOnly start,
        DateOnly? end = null,
        TravelStyle style = TravelStyle.Casual,
        IEnumerable<string>? interests = null)
    {
        var list = bases.ToList();
        var countries = list
            .Select(@base => new Country(@base.Name, @base.Days, new[] { @base }))
            .ToList();

        var arrival = new Arrival(countries[0].FirstBase.Name, start, new TimeOnly(9, 0));
        var departure = new Departure(
            countries[^1].LastBase.Name,
            end ?? start.AddDays(list.Sum(@base => @base.Days) - 1),
            new TimeOnly(18, 0));

        return TripBrief.Create(countries, arrival, departure, interests, style);
    }
}
