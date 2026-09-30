using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ApprovedActivityListTests
{
    private static ApprovedActivity Activity(
        string name,
        string destination = "Tokyo",
        bool approved = true,
        bool optional = true,
        string area = "Asakusa") =>
        new(name, area, destination, "2 hours", approved: approved, optional: optional);

    [Fact]
    public void ApprovedOnly_excludes_unapproved()
    {
        var list = new ApprovedActivityList(new[]
        {
            Activity("A", approved: true),
            Activity("B", approved: false),
        });

        list.ApprovedOnly().Select(a => a.Name).Should().Equal("A");
    }

    [Fact]
    public void MustDo_and_Fillers_partition_approved_activities()
    {
        var list = new ApprovedActivityList(new[]
        {
            Activity("Must", optional: false),
            Activity("Filler", optional: true),
            Activity("Hidden", approved: false, optional: false),
        });

        list.MustDo().Select(a => a.Name).Should().Equal("Must");
        list.Fillers().Select(a => a.Name).Should().Equal("Filler");
    }

    [Fact]
    public void ByDestination_is_case_insensitive_and_lists_must_dos_first()
    {
        var list = new ApprovedActivityList(new[]
        {
            Activity("Filler", destination: "tokyo", optional: true),
            Activity("Must", destination: "TOKYO", optional: false),
            Activity("Elsewhere", destination: "Kyoto"),
        });

        list.ByDestination("Tokyo").Select(a => a.Name).Should().Equal("Must", "Filler");
    }

    [Fact]
    public void FromSuggestion_copies_research_fields_and_marks_origin()
    {
        var suggestion = new Suggestion(
            "Senso-ji",
            "Tokyo",
            "Matches your interest in history",
            "Asakusa",
            "1 hour",
            openingHours: "6am-5pm",
            approximateCost: "Free",
            sourceLink: "https://example.com",
            sourceName: ResearchSourceName.Tavily,
            confidence: ConfidenceLevel.High);

        var activity = ApprovedActivity.FromSuggestion(suggestion, approved: true, optional: false, notes: "Go early");

        activity.Name.Should().Be("Senso-ji");
        activity.Area.Should().Be("Asakusa");
        activity.Destination.Should().Be("Tokyo");
        activity.Rationale.Should().Be("Matches your interest in history");
        activity.OpeningHours.Should().Be("6am-5pm");
        activity.ApproximateCost.Should().Be("Free");
        activity.SourceLink.Should().Be("https://example.com");
        activity.FoundVia.Should().Be(ResearchSourceName.Tavily);
        activity.Confidence.Should().Be(ConfidenceLevel.High);
        activity.Origin.Should().Be(ActivityOrigin.Research);
        activity.Optional.Should().BeFalse();
        activity.IsMustDo.Should().BeTrue();
        activity.Notes.Should().Be("Go early");
    }

    [Fact]
    public void Replace_swaps_the_whole_list()
    {
        var list = new ApprovedActivityList(new[] { Activity("A") });

        list.Replace(new[] { Activity("B"), Activity("C") });

        list.Activities.Select(a => a.Name).Should().Equal("B", "C");
    }
}
