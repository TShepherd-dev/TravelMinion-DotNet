using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public sealed class ResearchServiceTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    private static RawResult Raw(string title, string url = "https://example.com/a") =>
        new(title, url, "snippet", ResearchSourceName.Tavily);

    private static ResearchService BuildService(IReadOnlyList<RawResult> results) =>
        BuildService(new FakeResearchSource(results));

    private static ResearchService BuildService(IResearchSource primary)
    {
        var engine = new ResearchEngine(
            new FakeResearchEnricher(),
            new FakeUrlFetcher(),
            new FakeResearchSource(Array.Empty<RawResult>()),
            primary);

        return new ResearchService(engine, () => FixedNow);
    }

    private static TripBrief BriefWith(params Base[] bases)
        => TestBrief.FromBases(bases, new DateOnly(2027, 4, 1));

    [Fact]
    public async Task RunAsync_drives_a_queued_job_to_succeeded()
    {
        var service = BuildService(new[] { Raw("A"), Raw("B") });
        var job = ResearchJob.Queue(Guid.NewGuid(), FixedNow);

        var result = await service.RunAsync(job, BriefWith(new Base("Tokyo", 1)));

        job.Status.Should().Be(ResearchJobStatus.Succeeded);
        job.StartedAt.Should().Be(FixedNow);
        job.CompletedAt.Should().Be(FixedNow);
        result.Suggestions.Should().HaveCount(2);
        result.Job.Should().BeSameAs(job);
    }

    [Fact]
    public async Task RunAsync_records_progress_per_destination_in_order()
    {
        var service = BuildService(new[] { Raw("A") });
        var job = ResearchJob.Queue(Guid.NewGuid(), FixedNow);

        await service.RunAsync(job, BriefWith(
            new Base("Tokyo", 1),
            new Base("Kyoto", 1)));

        job.Progress.Select(progress => progress.Destination).Should().Equal("Tokyo", "Kyoto");
        job.Progress.Should().OnlyContain(progress => progress.Completed && progress.SuggestionsFound == 1);
    }

    [Fact]
    public async Task RunAsync_fails_the_job_and_rethrows_when_research_throws()
    {
        var service = BuildService(new FakeResearchSource(_ => throw new HttpRequestException("boom")));
        var job = ResearchJob.Queue(Guid.NewGuid(), FixedNow);

        var act = () => service.RunAsync(job, BriefWith(new Base("Tokyo", 1)));

        await act.Should().ThrowAsync<HttpRequestException>();
        job.Status.Should().Be(ResearchJobStatus.Failed);
        job.FailureReason.Should().Be("boom");
    }

    [Fact]
    public async Task RunAsync_cancels_the_job_and_returns_what_was_found()
    {
        var service = BuildService(new FakeResearchSource(_ => throw new OperationCanceledException()));
        var job = ResearchJob.Queue(Guid.NewGuid(), FixedNow);

        var result = await service.RunAsync(job, BriefWith(new Base("Tokyo", 1)));

        job.Status.Should().Be(ResearchJobStatus.Cancelled);
        result.Job.Should().BeSameAs(job);
        result.Suggestions.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_keeps_suggestions_found_before_cancellation()
    {
        var service = BuildService(new FakeResearchSource(destination =>
            destination == "Tokyo"
                ? new[] { Raw("A") }
                : throw new OperationCanceledException()));
        var job = ResearchJob.Queue(Guid.NewGuid(), FixedNow);

        var result = await service.RunAsync(job, BriefWith(
            new Base("Tokyo", 1),
            new Base("Kyoto", 1)));

        job.Status.Should().Be(ResearchJobStatus.Cancelled);
        result.Suggestions.Should().ContainSingle().Which.Name.Should().Be("A");
    }

    [Fact]
    public async Task RunAsync_reports_progress_through_the_reporter()
    {
        var reporter = new ResearchProgressReporter();
        var engine = new ResearchEngine(
            new FakeResearchEnricher(),
            new FakeUrlFetcher(),
            new FakeResearchSource(Array.Empty<RawResult>()),
            new FakeResearchSource(new[] { Raw("A") }),
            reporter);
        var service = new ResearchService(engine, () => FixedNow, reporter);
        var job = ResearchJob.Queue(Guid.NewGuid(), FixedNow);

        await service.RunAsync(job, BriefWith(new Base("Tokyo", 1)));

        reporter.Current.Stage.Should().Be(ResearchStage.Completed);
        reporter.Current.DestinationCount.Should().Be(1);
        reporter.Current.Suggestions.Should().ContainSingle().Which.Name.Should().Be("A");
    }

    [Fact]
    public async Task RunAsync_rejects_null_arguments()
    {
        var service = BuildService(Array.Empty<RawResult>());

        var act = () => service.RunAsync(null!, BriefWith(new Base("Tokyo", 1)));

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
