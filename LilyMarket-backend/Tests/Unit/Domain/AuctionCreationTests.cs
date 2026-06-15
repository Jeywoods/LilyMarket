using FluentAssertions;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.ValueObjects;
using Xunit;

namespace LilyMarket.Tests.Unit.Domain;

public class AuctionCreationTests
{
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly DateTime _now = DateTime.UtcNow;

    [Fact]
    public void CreateAuction_ValidData_CreatesSuccessfully()
    {
        var auction = new Auction(_sellerId, "Title", "Description", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddDays(7), _now);

        auction.Status.Should().Be(AuctionStatus.Active);
        auction.StartingPrice.Should().Be(100);
        auction.Category.Should().Be("Tech");
        auction.Condition.Should().Be("Good");
    }

    [Fact]
    public void CreateAuction_EmptyTitle_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddDays(7), _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAuction_WhitespaceTitle_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "   ", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddDays(7), _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAuction_EmptyDescription_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "Title", "", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddDays(7), _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAuction_EndTimeInPast_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddHours(-1), _now);
        act.Should().Throw<ArgumentException>().WithMessage("*future*");
    }

    [Fact]
    public void CreateAuction_EndTimeEqualNow_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now, _now);
        act.Should().Throw<ArgumentException>().WithMessage("*future*");
    }

    [Fact]
    public void CreateAuction_BuyNowLessThanStartingPrice_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), new Money(50), _now.AddDays(7), _now);
        act.Should().Throw<ArgumentException>().WithMessage("*BuyNow*");
    }

    [Fact]
    public void CreateAuction_BuyNowEqualStartingPrice_ThrowsArgumentException()
    {
        var act = () => new Auction(_sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), new Money(100), _now.AddDays(7), _now);
        act.Should().Throw<ArgumentException>().WithMessage("*BuyNow*");
    }

    [Fact]
    public void CreateAuction_WithBuyNow_CreatesSuccessfully()
    {
        var auction = new Auction(_sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), new Money(500), _now.AddDays(7), _now);
        auction.BuyNowPrice.Should().Be(500);
    }

    [Fact]
    public void CreateAuction_ZeroStartingPrice_ThrowsArgumentException()
    {
        var act = () => new Money(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAuction_NegativeStartingPrice_ThrowsArgumentException()
    {
        var act = () => new Money(-100);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAuction_ZeroMinIncrement_ThrowsArgumentException()
    {
        var act = () => new Money(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAuction_VeryLongTitle_StillCreates()
    {
        var longTitle = new string('A', 200);
        var auction = new Auction(_sellerId, longTitle, "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddDays(7), _now);
        auction.Title.Length.Should().Be(200);
    }

    [Fact]
    public void CreateAuction_EndTimeFarInFuture_CreatesSuccessfully()
    {
        var auction = new Auction(_sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, _now.AddDays(30), _now);
        auction.EndTime.Should().BeAfter(_now.AddDays(29));
    }
}