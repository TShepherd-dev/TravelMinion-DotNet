using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests.Domain;

public sealed class SuggestionMergeTests
{
    private static Suggestion Suggestion(string name, string destination = "Tokyo")
        => new(name, destination, "rationale", "area", "2 hours");

    [Fact]
    public void AppendSuggestions_adds_only_candidates_that_are_new()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");
        trip.ReplaceSuggestions([Suggestion("Ramen Tour")]);

        var appended = trip.AppendSuggestions([Suggestion("Ramen Tour"), Suggestion("Sushi Class")]);

        appended.Select(s => s.Name).Should().Equal("Sushi Class");
        trip.Suggestions.Select(s => s.Name).Should().Equal("Ramen Tour", "Sushi Class");
    }

    [Fact]
    public void AppendSuggestions_is_case_insensitive_and_destination_scoped()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");
        trip.ReplaceSuggestions([Suggestion("Ramen Tour", "Tokyo")]);

        var appended = trip.AppendSuggestions(
        [
            Suggestion("ramen tour", "TOKYO"),
            Suggestion("Ramen Tour", "Kyoto"),
        ]);

        appended.Select(s => s.Name).Should().Equal("Ramen Tour");
        appended[0].Destination.Should().Be("Kyoto");
    }

    [Fact]
    public void AppendSuggestions_does_not_resurrect_a_discarded_candidate()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");
        trip.ReplaceSuggestions([Suggestion("Ramen Tour")]);
        trip.DiscardSuggestions([0]);

        var appended = trip.AppendSuggestions([Suggestion("Ramen Tour")]);

        appended.Should().BeEmpty();
        trip.Suggestions.Should().ContainSingle();
    }

    [Fact]
    public void DiscardSuggestions_marks_only_valid_indices()
    {
        var trip = new Trip(Guid.NewGuid(), "Japan");
        trip.ReplaceSuggestions([Suggestion("A"), Suggestion("B"), Suggestion("C")]);

        trip.DiscardSuggestions([1, 99, -1]);

        trip.Suggestions.Select(s => s.Discarded).Should().Equal(false, true, false);
    }

    [Fact]
    public void ApprovedActivityList_Discard_soft_deletes_the_activity()
    {
        var activity = new ApprovedActivity("Museum", "Area", "Tokyo", "2 hours");
        var list = new ApprovedActivityList([activity]);

        list.Discard(activity);

        activity.Discarded.Should().BeTrue();
        list.Activities.Should().ContainSingle();
    }
}
