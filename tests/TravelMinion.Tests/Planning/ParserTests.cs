using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class DurationParserTests
{
    [Theory]
    [InlineData("2-3 hours", 150)]
    [InlineData("2 hours", 120)]
    [InlineData("30 min", 30)]
    [InlineData("half day", 240)]
    [InlineData("full day", 480)]
    [InlineData("2.5 hours", 150)]
    public void Parses_common_durations(string input, int expectedMinutes)
    {
        DurationParser.ToMinutes(input).Should().Be(expectedMinutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("whenever")]
    public void Falls_back_to_default_for_unparseable(string? input)
    {
        DurationParser.ToMinutes(input).Should().Be(PlannerConstants.DefaultActivityDuration);
    }
}

public class TransitDurationParserTests
{
    [Theory]
    [InlineData("flight 3h", 180)]
    [InlineData("train 2h15m", 135)]
    [InlineData("bus 4 hours", 240)]
    [InlineData("3h", 180)]
    [InlineData("90 mins", 90)]
    public void Parses_common_transits(string input, int expectedMinutes)
    {
        TransitDurationParser.ToMinutes(input).Should().Be(expectedMinutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("soon")]
    [InlineData("")]
    public void Returns_null_when_no_duration(string? input)
    {
        TransitDurationParser.ToMinutes(input).Should().BeNull();
    }
}

public class OpeningHoursParserTests
{
    [Fact]
    public void Parses_am_pm_range()
    {
        var result = OpeningHoursParser.Parse("9am-6pm");

        result.Should().Be((new TimeOnly(9, 0), new TimeOnly(18, 0)));
    }

    [Fact]
    public void Parses_24_hour_range()
    {
        var result = OpeningHoursParser.Parse("9:00-18:00");

        result.Should().Be((new TimeOnly(9, 0), new TimeOnly(18, 0)));
    }

    [Theory]
    [InlineData("24 hours")]
    [InlineData("always open")]
    [InlineData(null)]
    [InlineData("")]
    public void Returns_null_for_always_open_or_unparseable(string? input)
    {
        OpeningHoursParser.Parse(input).Should().BeNull();
    }
}

public class IndoorFallbackTests
{
    private static ApprovedActivity Activity(string name, string area = "City-wide", string? notes = null) =>
        new(name, area, "Tokyo", "2 hours", notes: notes);

    [Fact]
    public void Suggests_for_a_beach()
    {
        IndoorFallback.Suggest(Activity("Sunny Beach"))
            .Should().Be("Visit nearby aquarium or coastal museum");
    }

    [Fact]
    public void Suggests_generic_fallback_for_generic_outdoor()
    {
        IndoorFallback.Suggest(Activity("Mountain Lake Walk"))
            .Should().Be("Find nearby indoor attraction or cafe");
    }

    [Fact]
    public void Returns_null_for_indoor_activity()
    {
        IndoorFallback.Suggest(Activity("City Museum")).Should().BeNull();
    }

    [Fact]
    public void Returns_null_for_unrelated_activity()
    {
        IndoorFallback.Suggest(Activity("Chef's Table Dinner")).Should().BeNull();
    }
}
