using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ArrivalDepartureTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Arrival_rejects_blank_base_name(string name)
    {
        var act = () => new Arrival(name, new DateOnly(2027, 4, 1), new TimeOnly(9, 0));

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Departure_rejects_blank_base_name(string name)
    {
        var act = () => new Departure(name, new DateOnly(2027, 4, 1), new TimeOnly(9, 0));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Arrival_carries_base_date_and_time()
    {
        var arrival = new Arrival("  Tokyo  ", new DateOnly(2027, 2, 25), new TimeOnly(5, 25));

        arrival.BaseName.Should().Be("Tokyo");
        arrival.Date.Should().Be(new DateOnly(2027, 2, 25));
        arrival.Time.Should().Be(new TimeOnly(5, 25));
    }

    [Fact]
    public void Departure_carries_base_date_and_time()
    {
        var departure = new Departure("  Seoul  ", new DateOnly(2027, 3, 18), new TimeOnly(13, 45));

        departure.BaseName.Should().Be("Seoul");
        departure.Date.Should().Be(new DateOnly(2027, 3, 18));
        departure.Time.Should().Be(new TimeOnly(13, 45));
    }
}
