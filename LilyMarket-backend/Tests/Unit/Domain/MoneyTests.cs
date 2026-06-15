using FluentAssertions;
using LilyMarket.Domain.ValueObjects;
using Xunit;

namespace LilyMarket.Tests.Unit.Domain;

public class MoneyTests
{
    [Fact]
    public void PositiveAmount_CreatesSuccessfully()
    {
        var money = new Money(100);
        money.Amount.Should().Be(100);
    }

    [Fact]
    public void ZeroAmount_ThrowsArgumentException()
    {
        var act = () => new Money(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void NegativeAmount_ThrowsArgumentException()
    {
        var act = () => new Money(-50);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RoundsToTwoDecimals()
    {
        var money = new Money(100.456m);
        money.Amount.Should().Be(100.46m);
    }

    [Fact]
    public void Comparison_LessThan()
    {
        (new Money(10) < new Money(20)).Should().BeTrue();
        (new Money(20) < new Money(10)).Should().BeFalse();
    }

    [Fact]
    public void Comparison_GreaterThan()
    {
        (new Money(20) > new Money(10)).Should().BeTrue();
    }

    [Fact]
    public void Comparison_Equal()
    {
        (new Money(10.00m).Amount == new Money(10.00m).Amount).Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToDecimal()
    {
        decimal value = new Money(50);
        value.Should().Be(50);
    }

    [Fact]
    public void ExplicitConversion_FromDecimal()
    {
        Money money = (Money)50m;
        money.Amount.Should().Be(50);
    }

    [Fact]
    public void VeryLargeAmount_CreatesSuccessfully()
    {
        var money = new Money(999999999.99m);
        money.Amount.Should().Be(999999999.99m);
    }

    [Fact]
    public void VerySmallAmount_CreatesSuccessfully()
    {
        var money = new Money(0.01m);
        money.Amount.Should().Be(0.01m);
    }
}