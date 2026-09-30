using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class TripBriefTests
{
    [Fact]
    public void Create_requires_at_least_one_destination()
    {
        var act = () => TripBrief.Create(
            Array.Empty<DestinationStop>(),
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_end_before_start()
    {
        var act = () => TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1) },
            new DateOnly(2027, 4, 10),
            new DateOnly(2027, 4, 1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_non_positive_group_size()
    {
        var act = () => TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5),
            groupSize: 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Span_in_days_is_inclusive()
    {
        var brief = TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 5) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5));

        brief.SpanInDays.Should().Be(5);
    }

    [Fact]
    public void Distributes_days_evenly_when_sum_does_not_match_span()
    {
        var brief = TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1), new DestinationStop("Kyoto", 1) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 10));

        brief.Destinations.Select(d => d.Days).Should().Equal(5, 5);
    }

    [Fact]
    public void Gives_remainder_days_to_earlier_stops()
    {
        var brief = TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1), new DestinationStop("Kyoto", 1), new DestinationStop("Seoul", 1) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 10));

        brief.Destinations.Select(d => d.Days).Should().Equal(4, 3, 3);
    }

    [Fact]
    public void Keeps_explicit_days_when_they_sum_to_the_span()
    {
        var brief = TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 4), new DestinationStop("Kyoto", 6) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 10));

        brief.Destinations.Select(d => d.Days).Should().Equal(4, 6);
    }

    [Fact]
    public void Falls_back_to_default_interests_when_none_supplied()
    {
        var brief = TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5));

        brief.Interests.Should().Equal(DomainDefaults.Interests);
    }

    [Fact]
    public void Falls_back_to_default_interests_when_empty_list_supplied()
    {
        var brief = TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5),
            interests: Array.Empty<string>());

        brief.Interests.Should().Equal(DomainDefaults.Interests);
    }
}
