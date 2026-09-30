using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ItineraryTests
{
    [Fact]
    public void TimeBlock_rejects_end_before_start()
    {
        var act = () => new TimeBlock(new TimeOnly(10, 0), new TimeOnly(9, 0), "Museum", "Ueno", "1 hour");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GetDay_finds_a_day_by_date()
    {
        var day = new FreeDay(new DateOnly(2027, 4, 1), "Tokyo", "Rest");
        var itinerary = new Itinerary(new ItineraryDay[] { day });

        itinerary.GetDay(new DateOnly(2027, 4, 1)).Should().BeSameAs(day);
        itinerary.GetDay(new DateOnly(2027, 4, 2)).Should().BeNull();
    }

    [Fact]
    public void DateRange_spans_the_days()
    {
        var itinerary = new Itinerary(new ItineraryDay[]
        {
            new FreeDay(new DateOnly(2027, 4, 3), "Kyoto"),
            new FreeDay(new DateOnly(2027, 4, 1), "Tokyo"),
        });

        itinerary.DateRange.Should().Be((new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 3)));
    }

    [Fact]
    public void DateRange_is_null_when_empty()
    {
        new Itinerary().DateRange.Should().BeNull();
    }
}
