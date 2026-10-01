using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class BaseTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_name(string name)
    {
        var act = () => new Base(name, 1);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rejects_non_positive_days(int days)
    {
        var act = () => new Base("Tokyo", days);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Carries_name_days_and_transit()
    {
        var @base = new Base("Kyoto", 3, transitFromPrevious: "train 2h15m");

        @base.Name.Should().Be("Kyoto");
        @base.Days.Should().Be(3);
        @base.TransitFromPrevious.Should().Be("train 2h15m");
    }

    [Fact]
    public void Trims_name_and_transit()
    {
        var @base = new Base("  Kyoto  ", 3, transitFromPrevious: "  train 2h  ");

        @base.Name.Should().Be("Kyoto");
        @base.TransitFromPrevious.Should().Be("train 2h");
    }

    [Fact]
    public void Treats_blank_transit_as_null()
    {
        var @base = new Base("Kyoto", 3, transitFromPrevious: "   ");

        @base.TransitFromPrevious.Should().BeNull();
    }
}
