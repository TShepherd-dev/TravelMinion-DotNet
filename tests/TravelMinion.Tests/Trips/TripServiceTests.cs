using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests.Trips;

public sealed class TripServiceTests
{
    private static TripBrief Brief()
        => TripBrief.Create(
            new[] { new DestinationStop("Tokyo", 3), new DestinationStop("Kyoto", 2) },
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5),
            interests: new[] { "food", "history" });

    private static Suggestion Suggestion(string name, string destination)
        => new(
            name,
            destination,
            "Popular destination attraction",
            "City-wide",
            "2 hours",
            openingHours: "9am-5pm",
            approximateCost: "Free",
            sourceLink: "https://example.com",
            sourceName: ResearchSourceName.Tavily,
            confidence: ConfidenceLevel.High);

    [Fact]
    public async Task CreateAsync_persists_a_trip_with_its_brief()
    {
        var repository = new FakeTripRepository();
        var service = new TripService(repository);

        var trip = await service.CreateAsync("Japan Spring", Brief());

        trip.Name.Should().Be("Japan Spring");
        trip.Brief.Should().NotBeNull();
        repository.SaveCount.Should().Be(1);
        (await service.GetAsync(trip.Id)).Should().BeSameAs(trip);
    }

    [Fact]
    public async Task ApproveActivitiesAsync_builds_the_approved_list_from_selected_suggestions()
    {
        var repository = new FakeTripRepository();
        var service = new TripService(repository);
        var trip = await service.CreateAsync("Japan Spring", Brief());
        trip.ReplaceSuggestions(new[]
        {
            Suggestion("Senso-ji Temple", "Tokyo"),
            Suggestion("Fushimi Inari", "Kyoto"),
            Suggestion("Nishiki Market", "Kyoto"),
        });

        var updated = await service.ApproveActivitiesAsync(trip.Id, new[] { 0, 2 }, new[] { 2 });

        updated.Activities.Activities.Should().HaveCount(2);

        var mustDo = updated.Activities.MustDo().Single();
        mustDo.Name.Should().Be("Nishiki Market");
        mustDo.IsMustDo.Should().BeTrue();
        mustDo.Origin.Should().Be(ActivityOrigin.Research);

        var filler = updated.Activities.Fillers().Single();
        filler.Name.Should().Be("Senso-ji Temple");
        filler.Optional.Should().BeTrue();
        filler.Confidence.Should().Be(ConfidenceLevel.High);

        repository.SaveCount.Should().Be(2);
    }

    [Fact]
    public async Task ApproveActivitiesAsync_throws_for_an_unknown_trip()
    {
        var service = new TripService(new FakeTripRepository());

        var act = () => service.ApproveActivitiesAsync(
            Guid.NewGuid(),
            Array.Empty<int>(),
            Array.Empty<int>());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RenameAsync_changes_the_name_and_saves()
    {
        var repository = new FakeTripRepository();
        var service = new TripService(repository);
        var trip = await service.CreateAsync("Old name", Brief());

        var renamed = await service.RenameAsync(trip.Id, "New name");

        renamed.Name.Should().Be("New name");
        repository.SaveCount.Should().Be(2);
    }
}
