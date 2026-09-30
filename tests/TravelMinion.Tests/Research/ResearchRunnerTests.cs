using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;
using TravelMinion.Tests.Trips;

namespace TravelMinion.Tests;

public sealed class ResearchRunnerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static Trip TripWithBrief()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan Spring");
        trip.CaptureBrief(TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 1) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 1),
            interests: new[] { "food" }));
        return trip;
    }

    private static ResearchService Service(int suggestionCount)
    {
        var results = Enumerable.Range(0, suggestionCount)
            .Select(index => new RawResult(
                $"Attraction {index}",
                $"https://example.com/{index}",
                "A snippet",
                ResearchSourceName.Tavily))
            .ToList();

        var engine = new ResearchEngine(
            new FakeResearchEnricher(),
            new FakeUrlFetcher(),
            new FakeResearchSource(results));

        return new ResearchService(engine, () => Now);
    }

    [Fact]
    public async Task RunAsync_queues_a_job_appends_suggestions_and_saves()
    {
        var repository = new FakeTripRepository();
        var trip = TripWithBrief();
        var runner = new ResearchRunner(Service(3), repository, clock: () => Now);

        var result = await runner.RunAsync(trip);

        runner.IsAvailable.Should().BeTrue();
        result.Job.Status.Should().Be(ResearchJobStatus.Succeeded);
        result.Merged!.AppendedCount.Should().Be(3);
        trip.Suggestions.Should().HaveCount(3);
        trip.ResearchJobs.Should().ContainSingle()
            .Which.Status.Should().Be(ResearchJobStatus.Succeeded);
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_throws_when_the_trip_has_no_brief()
    {
        var runner = new ResearchRunner(Service(1), new FakeTripRepository(), clock: () => Now);
        var trip = new Trip(Guid.NewGuid(), "No brief");

        var act = () => runner.RunAsync(trip);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Unavailable_runner_is_not_available_and_throws()
    {
        var runner = new UnavailableResearchRunner();

        runner.IsAvailable.Should().BeFalse();
        var act = () => runner.RunAsync(TripWithBrief());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
