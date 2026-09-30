using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class TripTests
{
    private static readonly DateTimeOffset Now = new(2027, 4, 1, 9, 0, 0, TimeSpan.Zero);

    private static TripBrief Brief() => TripBrief.Create(
        new[] { new DestinationStop("Tokyo", 5) },
        new DateOnly(2027, 4, 1),
        new DateOnly(2027, 4, 5));

    [Fact]
    public void Requires_a_name()
    {
        var act = () => new Trip(Guid.NewGuid(), " ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CaptureBrief_stores_the_brief()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");
        var brief = Brief();

        trip.CaptureBrief(brief);

        trip.Brief.Should().BeSameAs(brief);
    }

    [Fact]
    public void QueueResearch_adds_and_returns_a_job()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");

        var job = trip.QueueResearch(Now);

        trip.ResearchJobs.Should().ContainSingle();
        trip.ResearchJobs[0].Should().BeSameAs(job);
        job.TripId.Should().Be(trip.Id);
    }

    [Fact]
    public void ReplaceSuggestions_replaces_the_previous_set()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");
        trip.ReplaceSuggestions(new[] { new Suggestion("Old", "Tokyo", "r", "area", "1 hour") });

        trip.ReplaceSuggestions(new[] { new Suggestion("New", "Tokyo", "r", "area", "1 hour") });

        trip.Suggestions.Select(s => s.Name).Should().Equal("New");
    }
}
