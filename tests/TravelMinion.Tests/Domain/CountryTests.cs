using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class CountryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_name(string name)
    {
        var act = () => new Country(name, 5, new[] { new Base("Tokyo", 5) });

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Rejects_non_positive_span(int span)
    {
        var act = () => new Country("Japan", span, new[] { new Base("Tokyo", 1) });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Requires_at_least_one_base()
    {
        var act = () => new Country("Japan", 5, Array.Empty<Base>());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_more_bases_than_days()
    {
        var act = () => new Country(
            "Japan",
            2,
            new[] { new Base("A", 1), new Base("B", 1), new Base("C", 1) });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Carries_name_span_and_ordered_bases()
    {
        var country = new Country("Japan", 10, new[] { new Base("Tokyo", 4), new Base("Kyoto", 6) });

        country.Name.Should().Be("Japan");
        country.SpanInDays.Should().Be(10);
        country.Bases.Select(b => b.Name).Should().Equal("Tokyo", "Kyoto");
    }

    [Fact]
    public void Exposes_first_and_last_base()
    {
        var country = new Country("Japan", 10, new[] { new Base("Tokyo", 4), new Base("Kyoto", 6) });

        country.FirstBase.Name.Should().Be("Tokyo");
        country.LastBase.Name.Should().Be("Kyoto");
    }

    [Fact]
    public void Distributes_base_days_evenly_when_sum_does_not_match_span()
    {
        var country = new Country("Japan", 10, new[] { new Base("Tokyo", 1), new Base("Kyoto", 1) });

        country.Bases.Select(b => b.Days).Should().Equal(5, 5);
    }

    [Fact]
    public void Gives_remainder_days_to_earlier_bases()
    {
        var country = new Country(
            "Japan",
            10,
            new[] { new Base("Tokyo", 1), new Base("Kyoto", 1), new Base("Osaka", 1) });

        country.Bases.Select(b => b.Days).Should().Equal(4, 3, 3);
    }

    [Fact]
    public void Keeps_explicit_base_days_when_they_sum_to_span()
    {
        var country = new Country("Japan", 10, new[] { new Base("Tokyo", 4), new Base("Kyoto", 6) });

        country.Bases.Select(b => b.Days).Should().Equal(4, 6);
    }
}
