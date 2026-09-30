using FluentAssertions;
using TravelMinion.Application;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class ConfidenceCalculatorTests
{
    [Fact]
    public void All_fields_present_is_high()
    {
        var enrichment = new SuggestionEnrichment("n", "r", "a", "d", "9am-5pm", "Free");

        var (confidence, couldntVerify) = ConfidenceCalculator.Calculate(enrichment, "https://example.com");

        confidence.Should().Be(ConfidenceLevel.High);
        couldntVerify.Should().BeNull();
    }

    [Fact]
    public void Missing_pricing_is_medium()
    {
        var enrichment = new SuggestionEnrichment("n", "r", "a", "d", "9am-5pm", null);

        var (confidence, couldntVerify) = ConfidenceCalculator.Calculate(enrichment, "https://example.com");

        confidence.Should().Be(ConfidenceLevel.Medium);
        couldntVerify.Should().Be("Couldn't verify pricing");
    }

    [Fact]
    public void Missing_source_link_is_medium()
    {
        var enrichment = new SuggestionEnrichment("n", "r", "a", "d", "9am-5pm", "Free");

        var (confidence, couldntVerify) = ConfidenceCalculator.Calculate(enrichment, null);

        confidence.Should().Be(ConfidenceLevel.Medium);
        couldntVerify.Should().Be("Couldn't verify source link");
    }

    [Fact]
    public void Missing_hours_and_pricing_is_low()
    {
        var enrichment = new SuggestionEnrichment("n", "r", "a", "d", null, null);

        var (confidence, couldntVerify) = ConfidenceCalculator.Calculate(enrichment, "https://example.com");

        confidence.Should().Be(ConfidenceLevel.Low);
        couldntVerify.Should().Be("Couldn't verify opening hours, pricing");
    }

    [Fact]
    public void Missing_everything_is_low()
    {
        var enrichment = new SuggestionEnrichment("n", "r", "a", "d");

        var (confidence, couldntVerify) = ConfidenceCalculator.Calculate(enrichment, null);

        confidence.Should().Be(ConfidenceLevel.Low);
        couldntVerify.Should().Be("Couldn't verify opening hours, pricing, source link");
    }
}
