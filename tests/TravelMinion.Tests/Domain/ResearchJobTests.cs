using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ResearchJobTests
{
    private static readonly Guid TripId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2027, 4, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Queue_starts_queued_with_the_attempt_count()
    {
        var job = ResearchJob.Queue(TripId, Now, attempt: 2);

        job.Status.Should().Be(ResearchJobStatus.Queued);
        job.AttemptCount.Should().Be(2);
        job.TripId.Should().Be(TripId);
        job.CreatedAt.Should().Be(Now);
        job.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Queue_rejects_a_negative_attempt()
    {
        var act = () => ResearchJob.Queue(TripId, Now, attempt: -1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Start_moves_queued_to_running()
    {
        var job = ResearchJob.Queue(TripId, Now);

        job.Start(Now.AddMinutes(1));

        job.Status.Should().Be(ResearchJobStatus.Running);
        job.StartedAt.Should().Be(Now.AddMinutes(1));
    }

    [Fact]
    public void Succeed_moves_running_to_succeeded()
    {
        var job = ResearchJob.Queue(TripId, Now);
        job.Start(Now);

        job.Succeed(Now.AddMinutes(5));

        job.Status.Should().Be(ResearchJobStatus.Succeeded);
        job.CompletedAt.Should().Be(Now.AddMinutes(5));
    }

    [Fact]
    public void Fail_records_the_reason()
    {
        var job = ResearchJob.Queue(TripId, Now);
        job.Start(Now);

        job.Fail("tavily unavailable", Now.AddMinutes(2));

        job.Status.Should().Be(ResearchJobStatus.Failed);
        job.FailureReason.Should().Be("tavily unavailable");
        job.CompletedAt.Should().Be(Now.AddMinutes(2));
    }

    [Fact]
    public void Cancel_moves_a_queued_job_to_cancelled()
    {
        var job = ResearchJob.Queue(TripId, Now);

        job.Cancel(Now);

        job.Status.Should().Be(ResearchJobStatus.Cancelled);
    }

    [Fact]
    public void RecordProgress_captures_per_destination_counts()
    {
        var job = ResearchJob.Queue(TripId, Now);
        job.Start(Now.AddMinutes(1));

        job.RecordProgress("Tokyo", 9);
        job.RecordProgress("Kyoto", 3, completed: false);

        job.Progress.Should().HaveCount(2);
        job.Progress[0].Destination.Should().Be("Tokyo");
        job.Progress[0].SuggestionsFound.Should().Be(9);
        job.Progress[0].Completed.Should().BeTrue();
        job.Progress[1].Completed.Should().BeFalse();
    }

    [Fact]
    public void Illegal_transitions_are_rejected()
    {
        var job = ResearchJob.Queue(TripId, Now);

        var startTwice = () =>
        {
            job.Start(Now);
            job.Start(Now);
        };

        startTwice.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cannot_succeed_a_queued_job()
    {
        var job = ResearchJob.Queue(TripId, Now);

        var act = () => job.Succeed(Now);

        act.Should().Throw<DomainException>();
    }
}
