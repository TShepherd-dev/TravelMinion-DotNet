using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public sealed class ResearchProgressReporterTests
{
    private static Suggestion Suggestion(string name) =>
        new(name, "Tokyo", "why it matches", "Area", "2 hours");

    [Fact]
    public void Starts_idle()
    {
        var reporter = new ResearchProgressReporter();

        reporter.Current.Should().Be(ResearchProgressState.Idle);
        reporter.Current.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Begin_starts_a_run_and_clears_previous_suggestions()
    {
        var reporter = new ResearchProgressReporter();
        reporter.Begin(1);
        reporter.AddSuggestions(new[] { Suggestion("Old") });

        reporter.Begin(3);

        reporter.Current.IsRunning.Should().BeTrue();
        reporter.Current.Stage.Should().Be(ResearchStage.SearchingSources);
        reporter.Current.DestinationCount.Should().Be(3);
        reporter.Current.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public void Reports_destination_stage_and_suggestions()
    {
        var reporter = new ResearchProgressReporter();
        reporter.Begin(2);

        reporter.SetDestination("Tokyo", 1);
        reporter.Stage(ResearchStage.ReadingPages);
        reporter.AddSuggestions(new[] { Suggestion("A"), Suggestion("B") });

        reporter.Current.Destination.Should().Be("Tokyo");
        reporter.Current.DestinationIndex.Should().Be(1);
        reporter.Current.Stage.Should().Be(ResearchStage.ReadingPages);
        reporter.Current.Suggestions.Select(suggestion => suggestion.Name).Should().Equal("A", "B");
    }

    [Fact]
    public void Raises_changed_on_every_update()
    {
        var reporter = new ResearchProgressReporter();
        var count = 0;
        reporter.Changed += () => count++;

        reporter.Begin(1);
        reporter.SetDestination("Tokyo", 1);
        reporter.Stage(ResearchStage.ReadingPages);
        reporter.AddSuggestions(new[] { Suggestion("A") });
        reporter.Finish(ResearchStage.Completed);

        count.Should().Be(5);
    }

    [Fact]
    public void Finish_stops_the_run()
    {
        var reporter = new ResearchProgressReporter();
        reporter.Begin(1);

        reporter.Finish(ResearchStage.Completed);

        reporter.Current.IsRunning.Should().BeFalse();
        reporter.Current.Stage.Should().Be(ResearchStage.Completed);
    }

    [Fact]
    public void Cancel_cancels_the_started_run()
    {
        var reporter = new ResearchProgressReporter();
        var token = reporter.StartRun();

        reporter.Cancel();

        token.IsCancellationRequested.Should().BeTrue();
    }
}
