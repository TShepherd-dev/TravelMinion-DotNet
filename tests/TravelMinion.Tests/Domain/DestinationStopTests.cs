using FluentAssertions;
using TravelMinion.Domain;

namespace TravelMinion.Tests;

public class DestinationStopTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_blank_destination(string destination)
    {
        var act = () => new DestinationStop(destination, 1);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Rejects_non_positive_days(int days)
    {
        var act = () => new DestinationStop("Tokyo", days);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Carries_order_and_transit()
    {
        var stop = new DestinationStop("Kyoto", 3, order: 1, transitFromPrevious: "train 2h15m");

        stop.Destination.Should().Be("Kyoto");
        stop.Days.Should().Be(3);
        stop.Order.Should().Be(1);
        stop.TransitFromPrevious.Should().Be("train 2h15m");
    }
}
