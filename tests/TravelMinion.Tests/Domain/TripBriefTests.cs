using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class TripBriefTests
{
    private static readonly DateOnly Start = new(2027, 4, 1);

    private static TripBrief CreateBrief(
        IEnumerable<Country> countries,
        DateOnly? start = null,
        DateOnly? end = null,
        IEnumerable<string>? interests = null,
        TravelStyle travelStyle = TravelStyle.Casual,
        int? groupSize = null)
    {
        var list = countries.ToList();
        var from = start ?? Start;
        var to = end ?? from.AddDays(9);
        var arrival = new Arrival(list[0].FirstBase.Name, from, new TimeOnly(9, 0));
        var departure = new Departure(list[^1].LastBase.Name, to, new TimeOnly(18, 0));
        return TripBrief.Create(
            list,
            arrival,
            departure,
            interests,
            travelStyle,
            groupSize: groupSize);
    }

    [Fact]
    public void Create_requires_at_least_one_country()
    {
        var act = () => TripBrief.Create(
            Array.Empty<Country>(),
            new Arrival("Tokyo", Start, new TimeOnly(9, 0)),
            new Departure("Tokyo", Start.AddDays(4), new TimeOnly(18, 0)));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_departure_before_arrival()
    {
        var country = new Country("Japan", 5, new[] { new Base("Tokyo", 5) });

        var act = () => TripBrief.Create(
            new[] { country },
            new Arrival("Tokyo", new DateOnly(2027, 4, 10), new TimeOnly(9, 0)),
            new Departure("Tokyo", new DateOnly(2027, 4, 1), new TimeOnly(18, 0)));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_non_positive_group_size()
    {
        var act = () => CreateBrief(
            new[] { new Country("Japan", 5, new[] { new Base("Tokyo", 5) }) },
            groupSize: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_arrival_at_a_base_other_than_the_first()
    {
        var japan = new Country("Japan", 5, new[] { new Base("Tokyo", 2), new Base("Kyoto", 3) });

        var act = () => TripBrief.Create(
            new[] { japan },
            new Arrival("Kyoto", Start, new TimeOnly(9, 0)),
            new Departure("Kyoto", Start.AddDays(4), new TimeOnly(18, 0)));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_departure_from_a_base_other_than_the_last()
    {
        var japan = new Country("Japan", 5, new[] { new Base("Tokyo", 2), new Base("Kyoto", 3) });

        var act = () => TripBrief.Create(
            new[] { japan },
            new Arrival("Tokyo", Start, new TimeOnly(9, 0)),
            new Departure("Tokyo", Start.AddDays(4), new TimeOnly(18, 0)));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Derives_start_end_and_span_from_arrival_and_departure()
    {
        var brief = CreateBrief(
            new[] { new Country("Japan", 5, new[] { new Base("Tokyo", 5) }) },
            start: new DateOnly(2027, 4, 1),
            end: new DateOnly(2027, 4, 5));

        brief.StartDate.Should().Be(new DateOnly(2027, 4, 1));
        brief.EndDate.Should().Be(new DateOnly(2027, 4, 5));
        brief.SpanInDays.Should().Be(5);
    }

    [Fact]
    public void Distributes_country_spans_evenly_when_sum_does_not_match_trip_span()
    {
        var brief = CreateBrief(
            new[]
            {
                new Country("Japan", 1, new[] { new Base("Tokyo", 1) }),
                new Country("Korea", 1, new[] { new Base("Seoul", 1) }),
            },
            start: new DateOnly(2027, 4, 1),
            end: new DateOnly(2027, 4, 10));

        brief.Countries.Select(c => c.SpanInDays).Should().Equal(5, 5);
    }

    [Fact]
    public void Gives_remainder_days_to_earlier_countries()
    {
        var brief = CreateBrief(
            new[]
            {
                new Country("Japan", 1, new[] { new Base("Tokyo", 1) }),
                new Country("Korea", 1, new[] { new Base("Seoul", 1) }),
                new Country("China", 1, new[] { new Base("Beijing", 1) }),
            },
            start: new DateOnly(2027, 4, 1),
            end: new DateOnly(2027, 4, 10));

        brief.Countries.Select(c => c.SpanInDays).Should().Equal(4, 3, 3);
    }

    [Fact]
    public void Redistributes_bases_when_country_spans_change()
    {
        var brief = CreateBrief(
            new[]
            {
                new Country("Japan", 2, new[] { new Base("Tokyo", 1), new Base("Kyoto", 1) }),
                new Country("Korea", 2, new[] { new Base("Seoul", 1), new Base("Busan", 1) }),
            },
            start: new DateOnly(2027, 4, 1),
            end: new DateOnly(2027, 4, 10));

        brief.Countries[0].SpanInDays.Should().Be(5);
        brief.Countries[0].Bases.Select(b => b.Days).Should().Equal(3, 2);
        brief.Countries[1].SpanInDays.Should().Be(5);
        brief.Countries[1].Bases.Select(b => b.Days).Should().Equal(3, 2);
    }

    [Fact]
    public void Keeps_explicit_spans_and_days_when_they_sum()
    {
        var brief = CreateBrief(
            new[]
            {
                new Country("Japan", 4, new[] { new Base("Tokyo", 4) }),
                new Country("Korea", 6, new[] { new Base("Seoul", 6) }),
            },
            start: new DateOnly(2027, 4, 1),
            end: new DateOnly(2027, 4, 10));

        brief.Countries.Select(c => c.SpanInDays).Should().Equal(4, 6);
        brief.Countries[0].Bases.Select(b => b.Days).Should().Equal(4);
        brief.Countries[1].Bases.Select(b => b.Days).Should().Equal(6);
    }

    [Fact]
    public void Rejects_duplicate_country_names()
    {
        var act = () => CreateBrief(
            new[]
            {
                new Country("Japan", 5, new[] { new Base("Tokyo", 5) }),
                new Country("Japan", 5, new[] { new Base("Osaka", 5) }),
            });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_duplicate_base_names_across_countries()
    {
        var act = () => CreateBrief(
            new[]
            {
                new Country("Japan", 5, new[] { new Base("Tokyo", 5) }),
                new Country("Korea", 5, new[] { new Base("Tokyo", 5) }),
            });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_duplicate_base_names_within_a_country()
    {
        var act = () => CreateBrief(
            new[] { new Country("Japan", 5, new[] { new Base("Tokyo", 2), new Base("Tokyo", 3) }) });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_a_trip_shorter_than_its_country_count()
    {
        var act = () => CreateBrief(
            new[]
            {
                new Country("Japan", 1, new[] { new Base("Tokyo", 1) }),
                new Country("Korea", 1, new[] { new Base("Seoul", 1) }),
            },
            start: new DateOnly(2027, 4, 1),
            end: new DateOnly(2027, 4, 1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Falls_back_to_default_interests_when_none_supplied()
    {
        var brief = CreateBrief(new[] { new Country("Japan", 5, new[] { new Base("Tokyo", 5) }) });

        brief.Interests.Should().Equal(DomainDefaults.Interests);
    }

    [Fact]
    public void Falls_back_to_default_interests_when_empty_list_supplied()
    {
        var brief = CreateBrief(
            new[] { new Country("Japan", 5, new[] { new Base("Tokyo", 5) }) },
            interests: Array.Empty<string>());

        brief.Interests.Should().Equal(DomainDefaults.Interests);
    }
}
