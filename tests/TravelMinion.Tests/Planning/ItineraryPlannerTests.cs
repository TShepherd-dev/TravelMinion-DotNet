using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ItineraryPlannerTests
{
    private static readonly DateOnly Start = new(2027, 4, 1);

    private static TripBrief Brief(
        IEnumerable<DestinationStop> stops,
        DateOnly? end = null,
        TravelStyle style = TravelStyle.Casual) =>
        TripBrief.Create(
            stops,
            Start,
            end ?? Start.AddDays(4),
            travelStyle: style);

    private static ApprovedActivity Activity(
        string name,
        string destination = "Tokyo",
        string area = "Asakusa",
        string duration = "2 hours",
        string? openingHours = null,
        bool optional = true) =>
        new(name, area, destination, duration, openingHours: openingHours, optional: optional);

    [Fact]
    public void Splits_activities_across_days_by_density()
    {
        var brief = Brief(new[] { new DestinationStop("Tokyo", 2) }, Start.AddDays(1));
        var activities = new ApprovedActivityList(new[]
        {
            Activity("A"), Activity("B"), Activity("C"),
        });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        itinerary.Days.Should().HaveCount(2);
        itinerary.Days.Should().AllBeOfType<ActivityDay>();
        itinerary.Days.Cast<ActivityDay>().Sum(d => d.TimeBlocks.Count).Should().Be(3);
    }

    [Fact]
    public void Nothing_style_produces_only_free_days()
    {
        var brief = Brief(new[] { new DestinationStop("Tokyo", 2) }, Start.AddDays(1), TravelStyle.Nothing);
        var activities = new ApprovedActivityList(new[] { Activity("A") });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        itinerary.Days.Should().HaveCount(2);
        itinerary.Days.Should().AllBeOfType<FreeDay>();
        itinerary.Days.Cast<FreeDay>().Should().OnlyContain(d => d.Notes == "Rest day");
    }

    [Fact]
    public void Destination_without_activities_gets_free_days()
    {
        var brief = Brief(new[] { new DestinationStop("Tokyo", 3) }, Start.AddDays(2));
        var activities = new ApprovedActivityList();

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        itinerary.Days.Should().HaveCount(3);
        itinerary.Days.Cast<FreeDay>().Should().OnlyContain(d => d.Notes == "No planned activities");
    }

    [Fact]
    public void Short_transit_becomes_a_travel_day_with_a_leg_and_afternoon_activity()
    {
        var brief = Brief(new[]
        {
            new DestinationStop("Tokyo", 1, order: 0),
            new DestinationStop("Kyoto", 1, order: 1, transitFromPrevious: "train 2h15m"),
        }, Start.AddDays(1));
        var activities = new ApprovedActivityList(new[] { Activity("Temple", "Kyoto") });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        var travelDay = itinerary.Days.OfType<TravelDay>().Should().ContainSingle().Subject;
        travelDay.Destination.Should().Be("Kyoto");
        travelDay.TravelLeg.FromDestination.Should().Be("Tokyo");
        travelDay.TravelLeg.ToDestination.Should().Be("Kyoto");
        travelDay.TravelLeg.Mode.Should().Be("train");
        travelDay.TravelLeg.Duration.Should().Be("2h15m");
        travelDay.AfternoonActivity.Should().NotBeNull();
        travelDay.AfternoonActivity!.ActivityName.Should().Be("Explore Kyoto");
    }

    [Fact]
    public void Long_haul_transit_becomes_a_recovery_free_day()
    {
        var brief = Brief(new[]
        {
            new DestinationStop("Tokyo", 1, order: 0),
            new DestinationStop("Seoul", 1, order: 1, transitFromPrevious: "flight 7h"),
        }, Start.AddDays(1));
        var activities = new ApprovedActivityList(new[] { Activity("Palace", "Seoul") });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        itinerary.Days.OfType<TravelDay>().Should().BeEmpty();
        var recovery = itinerary.Days.OfType<FreeDay>()
            .Single(d => d.Notes!.Contains("Recovery day after long travel"));
        recovery.Destination.Should().Be("Seoul");
    }

    [Fact]
    public void Respects_opening_hours_by_pushing_start_to_opening()
    {
        var brief = Brief(new[] { new DestinationStop("Tokyo", 1) }, Start);
        var activities = new ApprovedActivityList(new[]
        {
            Activity("Late Opener", openingHours: "11am-6pm"),
        });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        var block = itinerary.Days.OfType<ActivityDay>().Single().TimeBlocks.Single();
        block.StartTime.Should().Be(new TimeOnly(11, 0));
        block.EndTime.Should().Be(new TimeOnly(13, 0));
    }

    [Fact]
    public void Assigns_an_indoor_fallback_to_weather_exposed_activities()
    {
        var brief = Brief(new[] { new DestinationStop("Tokyo", 1) }, Start);
        var activities = new ApprovedActivityList(new[] { Activity("Sunny Beach") });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        var block = itinerary.Days.OfType<ActivityDay>().Single().TimeBlocks.Single();
        block.IndoorFallback.Should().Be("Visit nearby aquarium or coastal museum");
    }

    [Fact]
    public void Schedules_must_dos_before_fillers_within_a_destination()
    {
        var brief = Brief(new[] { new DestinationStop("Tokyo", 1) }, Start);
        var activities = new ApprovedActivityList(new[]
        {
            Activity("Filler", optional: true),
            Activity("Must", optional: false),
        });

        var itinerary = new ItineraryPlanner(brief, activities).Plan();

        var blocks = itinerary.Days.OfType<ActivityDay>().Single().TimeBlocks;
        blocks[0].ActivityName.Should().Be("Must");
    }
}
