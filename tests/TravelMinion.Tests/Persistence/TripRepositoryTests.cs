using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TravelMinion.Application;
using TravelMinion.Domain;
using TravelMinion.Infrastructure.Persistence;

namespace TravelMinion.Tests.Persistence;

public sealed class TripRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=TravelMinionTests;Trusted_Connection=True;TrustServerCertificate=True";

    private static TravelMinionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TravelMinionDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        var context = new TravelMinionDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        return context;
    }

    private static Trip BuildTrip()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan Spring");

        var brief = TripBrief.Create(
            [
                new DestinationStop("Tokyo", 3),
                new DestinationStop("Kyoto", 2, transitFromPrevious: "train 2h15m"),
            ],
            new DateOnly(2027, 4, 1),
            new DateOnly(2027, 4, 5),
            interests: ["food", "history"],
            travelStyle: TravelStyle.Packed,
            budget: "moderate",
            groupSize: 2,
            preferredSources: ["https://example.com/guide"]);

        trip.CaptureBrief(brief);

        var job = trip.QueueResearch(Now);
        job.Start(Now);
        job.RecordProgress("Tokyo", 4);
        job.RecordProgress("Kyoto", 2);
        job.Succeed(Now);

        var suggestion = new Suggestion(
            "Senso-ji Temple",
            "Tokyo",
            "Matches your interest in history",
            "Asakusa",
            "2-3 hours",
            openingHours: "9am-5pm",
            approximateCost: "Free",
            seasonWeatherFit: "Good year-round",
            sourceLink: "https://example.com/sensoji",
            sourceName: ResearchSourceName.Tavily,
            confidence: ConfidenceLevel.High);

        trip.ReplaceSuggestions([suggestion]);

        trip.SetActivities(new ApprovedActivityList(
        [
            ApprovedActivity.FromSuggestion(suggestion, optional: false, notes: "Go early"),
        ]));

        trip.SetItinerary(new Itinerary(
        [
            new ActivityDay(
                new DateOnly(2027, 4, 1),
                "Tokyo",
                [new TimeBlock(new TimeOnly(9, 0), new TimeOnly(11, 0), "Senso-ji Temple", "Asakusa", "2 hours")]),
            new TravelDay(
                new DateOnly(2027, 4, 2),
                "Kyoto",
                new TravelLeg("Tokyo", "Kyoto", "train", "2h15m"),
                new TimeBlock(new TimeOnly(15, 0), new TimeOnly(17, 0), "Explore Kyoto", "Kyoto", "2 hours")),
            new FreeDay(new DateOnly(2027, 4, 3), "Kyoto", "Rest day"),
        ]));

        return trip;
    }

    [Fact]
    public async Task Round_trips_the_whole_aggregate()
    {
        await using var context = CreateContext();
        {
            var repository = new TripRepository(context);
            var trip = BuildTrip();

            await repository.AddAsync(trip);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var loaded = await repository.GetAsync(trip.Id);

            loaded.Should().NotBeNull();
            loaded!.Name.Should().Be("Japan Spring");

            loaded.Brief.Should().NotBeNull();
            loaded.Brief!.Destinations.Should().HaveCount(2);
            loaded.Brief.Destinations[0].Destination.Should().Be("Tokyo");
            loaded.Brief.Destinations[0].Days.Should().Be(3);
            loaded.Brief.Destinations[1].TransitFromPrevious.Should().Be("train 2h15m");
            loaded.Brief.Interests.Should().Equal("food", "history");
            loaded.Brief.PreferredSources.Should().Equal("https://example.com/guide");
            loaded.Brief.TravelStyle.Should().Be(TravelStyle.Packed);
            loaded.Brief.GroupSize.Should().Be(2);
            loaded.Brief.Budget.Should().Be("moderate");

            loaded.Suggestions.Should().ContainSingle();
            loaded.Suggestions[0].Name.Should().Be("Senso-ji Temple");
            loaded.Suggestions[0].SourceName.Should().Be(ResearchSourceName.Tavily);
            loaded.Suggestions[0].Confidence.Should().Be(ConfidenceLevel.High);
            loaded.Suggestions[0].OpeningHours.Should().Be("9am-5pm");
            loaded.Suggestions[0].Discarded.Should().BeFalse();

            loaded.ResearchJobs.Should().ContainSingle();
            var job = loaded.ResearchJobs[0];
            job.Status.Should().Be(ResearchJobStatus.Succeeded);
            job.Progress.Should().HaveCount(2);
            job.Progress[0].Destination.Should().Be("Tokyo");
            job.Progress[0].SuggestionsFound.Should().Be(4);

            loaded.Activities.Activities.Should().ContainSingle();
            var activity = loaded.Activities.Activities[0];
            activity.Origin.Should().Be(ActivityOrigin.Research);
            activity.Optional.Should().BeFalse();
            activity.IsMustDo.Should().BeTrue();
            activity.Notes.Should().Be("Go early");

            loaded.Itinerary.Should().NotBeNull();
            loaded.Itinerary!.Days.Should().HaveCount(3);
            loaded.Itinerary.GetDay(new DateOnly(2027, 4, 1)).Should().BeOfType<ActivityDay>();
            loaded.Itinerary.GetDay(new DateOnly(2027, 4, 2)).Should().BeOfType<TravelDay>();
            loaded.Itinerary.GetDay(new DateOnly(2027, 4, 3)).Should().BeOfType<FreeDay>();

            var travelDay = (TravelDay)loaded.Itinerary.GetDay(new DateOnly(2027, 4, 2))!;
            travelDay.TravelLeg.Mode.Should().Be("train");
            travelDay.TravelLeg.Duration.Should().Be("2h15m");
            travelDay.AfternoonActivity!.ActivityName.Should().Be("Explore Kyoto");

            var activityDay = (ActivityDay)loaded.Itinerary.GetDay(new DateOnly(2027, 4, 1))!;
            activityDay.TimeBlocks[0].StartTime.Should().Be(new TimeOnly(9, 0));
        }
    }

    [Fact]
    public async Task Lists_trips_ordered_by_name()
    {
        await using var context = CreateContext();
        {
            var repository = new TripRepository(context);

            await repository.AddAsync(new Trip(Guid.NewGuid(), "Zermatt"));
            await repository.AddAsync(new Trip(Guid.NewGuid(), "Amalfi"));
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var trips = await repository.ListAsync();

            trips.Select(t => t.Name).Should().Equal("Amalfi", "Zermatt");
        }
    }

    [Fact]
    public async Task Persists_a_trip_with_no_brief_or_research()
    {
        await using var context = CreateContext();
        {
            var repository = new TripRepository(context);
            var trip = new Trip(Guid.NewGuid(), "Blank slate");

            await repository.AddAsync(trip);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var loaded = await repository.GetAsync(trip.Id);

            loaded.Should().NotBeNull();
            loaded!.Brief.Should().BeNull();
            loaded.Suggestions.Should().BeEmpty();
            loaded.ResearchJobs.Should().BeEmpty();
            loaded.Itinerary.Should().BeNull();
        }
    }

    [Fact]
    public async Task Running_research_twice_appends_and_keeps_approvals_and_prior_jobs()
    {
        await using var context = CreateContext();
        {
            var repository = new TripRepository(context);

            var trip = new Trip(Guid.NewGuid(), "Repeat research");
            trip.CaptureBrief(TripBrief.Create(
                [new DestinationStop("Tokyo", 1)],
                new DateOnly(2027, 4, 1),
                new DateOnly(2027, 4, 1),
                interests: ["food"]));
            await repository.AddAsync(trip);
            await repository.SaveChangesAsync();

            // First research run, then approve one suggestion.
            var firstService = new ResearchService(FakeEngine(["Ramen Tour"]), () => Now);
            var runner = new ResearchRunner(firstService, repository, clock: () => Now);
            await runner.RunAsync(trip);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var loaded = await repository.GetAsync(trip.Id);
            loaded!.SetActivities(new ApprovedActivityList(
                [ApprovedActivity.FromSuggestion(loaded.Suggestions[0], optional: false)]));
            repository.Update(loaded);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            // Second run returns one duplicate and one brand-new suggestion.
            var second = await repository.GetAsync(trip.Id);
            var secondService = new ResearchService(FakeEngine(["Ramen Tour", "Sushi Class"]), () => Now);
            var secondRunner = new ResearchRunner(secondService, repository, clock: () => Now);
            var result = await secondRunner.RunAsync(second!);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            result.Merged!.AppendedCount.Should().Be(1);
            result.Merged.DuplicateCount.Should().Be(1);

            var reloaded = await repository.GetAsync(trip.Id);

            // The list grew rather than being replaced, and the approval survived.
            reloaded!.Suggestions.Select(s => s.Name).Should().Equal("Ramen Tour", "Sushi Class");
            reloaded.ResearchJobs.Should().HaveCount(2);
            reloaded.Activities.Activities.Should().ContainSingle()
                .Which.IsMustDo.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Discard_flags_round_trip_for_suggestions_and_approved_activities()
    {
        await using var context = CreateContext();
        {
            var repository = new TripRepository(context);

            var trip = new Trip(Guid.NewGuid(), "Discard round-trip");
            trip.CaptureBrief(TripBrief.Create(
                [new DestinationStop("Tokyo", 1)],
                new DateOnly(2027, 4, 1),
                new DateOnly(2027, 4, 1),
                interests: ["food"]));

            var suggestion = new Suggestion(
                "Ramen Tour",
                "Tokyo",
                "Matches your interest in food",
                "Shibuya",
                "2 hours");
            trip.ReplaceSuggestions([suggestion]);

            var activity = ApprovedActivity.FromSuggestion(suggestion, optional: false);
            trip.SetActivities(new ApprovedActivityList([activity]));

            trip.DiscardSuggestions([0]);
            trip.Activities.Discard(activity);

            await repository.AddAsync(trip);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var loaded = await repository.GetAsync(trip.Id);

            loaded!.Suggestions.Should().ContainSingle()
                .Which.Discarded.Should().BeTrue();
            loaded.Activities.Activities.Should().ContainSingle()
                .Which.Discarded.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Deleting_a_trip_removes_its_whole_graph()
    {
        await using var context = CreateContext();
        {
            var repository = new TripRepository(context);
            var trip = BuildTrip();

            await repository.AddAsync(trip);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var loaded = await repository.GetAsync(trip.Id);
            repository.Remove(loaded!);
            await repository.SaveChangesAsync();
            context.ChangeTracker.Clear();

            (await repository.GetAsync(trip.Id)).Should().BeNull();
            (await repository.ListAsync()).Should().BeEmpty();
            context.ResearchJobs.Should().BeEmpty();
        }
    }

    private static ResearchEngine FakeEngine(IReadOnlyList<string> titles)
    {
        var results = titles
            .Select(title => new RawResult(title, $"https://example.com/{title}", "Snippet", ResearchSourceName.Tavily))
            .ToList();

        return new ResearchEngine(
            new FakeResearchEnricher(),
            new FakeUrlFetcher(),
            new FakeResearchSource(results));
    }
}
