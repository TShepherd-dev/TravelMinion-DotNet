using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class InterviewServiceTests
{
    private static readonly DateOnly Today = new(2027, 1, 1);

    [Fact]
    public async Task StartAsync_returns_the_draft_and_its_follow_up_questions()
    {
        var draft = new TripBriefDraft
        {
            Destinations = new[] { new DestinationDraft("Tokyo") },
        };
        var extractor = new FakeTripBriefExtractor(draft);
        var service = new InterviewService(extractor);

        var result = await service.StartAsync("A week in Tokyo");

        extractor.LastFreeform.Should().Be("A week in Tokyo");
        result.Draft.Should().BeSameAs(draft);
        result.NeedsMoreInformation.Should().BeTrue();
        result.Questions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task StartAsync_rejects_a_blank_description()
    {
        var service = new InterviewService(new FakeTripBriefExtractor(new TripBriefDraft()));

        var act = async () => await service.StartAsync("   ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void BuildQuestions_asks_for_every_missing_required_field()
    {
        var questions = InterviewService.BuildQuestions(new TripBriefDraft());

        questions.Should().HaveCount(4);
    }

    [Fact]
    public void BuildQuestions_combines_missing_start_and_end_into_one_question()
    {
        var draft = new TripBriefDraft
        {
            Destinations = new[] { new DestinationDraft("Tokyo") },
            Interests = new[] { "food" },
            TravelStyle = TravelStyle.Casual,
        };

        var questions = InterviewService.BuildQuestions(draft);

        questions.Should().ContainSingle();
        questions[0].Should().Contain("travel dates");
    }

    [Fact]
    public void BuildQuestions_is_empty_when_the_draft_is_complete()
    {
        var draft = CompleteDraft();

        InterviewService.BuildQuestions(draft).Should().BeEmpty();
        draft.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void Finalize_applies_defaults_for_a_sparse_draft()
    {
        var brief = InterviewService.Finalize(new TripBriefDraft(), Today);

        brief.Destinations.Single().Destination.Should().Be("TBD");
        brief.StartDate.Should().Be(Today.AddDays(180));
        brief.EndDate.Should().Be(Today.AddDays(187));
        brief.Interests.Should().Equal(DomainDefaults.Interests);
        brief.TravelStyle.Should().Be(TravelStyle.Casual);
    }

    [Fact]
    public void Finalize_preserves_supplied_values()
    {
        var draft = CompleteDraft();

        var brief = InterviewService.Finalize(draft, Today);

        brief.Destinations.Single().Destination.Should().Be("Tokyo");
        brief.StartDate.Should().Be(new DateOnly(2027, 4, 1));
        brief.EndDate.Should().Be(new DateOnly(2027, 4, 5));
        brief.TravelStyle.Should().Be(TravelStyle.Packed);
        brief.Interests.Should().Equal("food", "history");
    }

    private static TripBriefDraft CompleteDraft() => new()
    {
        Destinations = new[] { new DestinationDraft("Tokyo", 5) },
        StartDate = new DateOnly(2027, 4, 1),
        EndDate = new DateOnly(2027, 4, 5),
        Interests = new[] { "food", "history" },
        TravelStyle = TravelStyle.Packed,
    };
}
