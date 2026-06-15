using FluentAssertions;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.Events;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;
using Xunit;

namespace LilyMarket.Tests.Unit.Domain;

public class AuctionEndingTests
{
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _bidderId = Guid.NewGuid();
    private readonly DateTime _now;

    public AuctionEndingTests()
    {
        _now = DateTime.UtcNow;
    }

    private Auction CreateActiveAuction()
    {
        return new Auction(_sellerId, "Test Item", "Test Description", "Tech", "Good",
            "https://example.com/photo.jpg", new Money(100), new Money(10), null, _now.AddDays(7), _now);
    }

    [Fact]
    public void End_WithBids_SetsWinnerToHighestBidder()
    {
        var auction = CreateActiveAuction();
        auction.PlaceBid(new Bid(auction.Id, _bidderId, new Money(150), _now), _now);
        auction.End(_now);
        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.DomainEvents.Should().Contain(e => e is AuctionEndedEvent);
        var endedEvent = auction.DomainEvents.OfType<AuctionEndedEvent>().First();
        endedEvent.WinnerId.Should().Be(_bidderId);
        endedEvent.WinningAmount.Should().Be(150);
    }

    [Fact]
    public void End_WithoutBids_NoWinnerSet_StatusIsEnded()
    {
        var auction = CreateActiveAuction();
        auction.End(_now);
        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.DomainEvents.Should().Contain(e => e is AuctionEndedNoWinnerEvent);
    }

    [Fact]
    public void End_WhenBuyNowTriggered_StatusIsSold()
    {
        var auction = new Auction(_sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), new Money(200), _now.AddDays(7), _now);
        auction.PlaceBid(new Bid(auction.Id, _bidderId, new Money(200), _now), _now);
        auction.Status.Should().Be(AuctionStatus.Sold);
    }

    [Fact]
    public void End_OnAlreadyEnded_DoesNothing()
    {
        var auction = CreateActiveAuction();
        auction.End(_now);
        auction.ClearDomainEvents();
        auction.End(_now);
        auction.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void End_OnCanceled_DoesNothing()
    {
        var auction = CreateActiveAuction();
        auction.Cancel(_sellerId);
        auction.End(_now);
        auction.Status.Should().Be(AuctionStatus.Canceled);
    }

    [Fact]
    public void Cancel_WithNoBids_StatusIsCanceled()
    {
        var auction = CreateActiveAuction();
        auction.Cancel(_sellerId);
        auction.Status.Should().Be(AuctionStatus.Canceled);
    }

    [Fact]
    public void Cancel_WithBids_ThrowsUnauthorizedOperationException()
    {
        var auction = CreateActiveAuction();
        auction.PlaceBid(new Bid(auction.Id, _bidderId, new Money(150), _now), _now);
        var act = () => auction.Cancel(_sellerId);
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void Cancel_ByNonSeller_ThrowsUnauthorizedOperationException()
    {
        var auction = CreateActiveAuction();
        var act = () => auction.Cancel(Guid.NewGuid());
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void Cancel_ByRandomUser_ThrowsUnauthorizedOperationException()
    {
        var auction = CreateActiveAuction();
        var act = () => auction.Cancel(Guid.NewGuid());
        act.Should().Throw<UnauthorizedOperationException>();
    }

    [Fact]
    public void End_WithMultipleBids_HighestWins()
    {
        var auction = CreateActiveAuction();
        var lowBidder = Guid.NewGuid();
        auction.PlaceBid(new Bid(auction.Id, lowBidder, new Money(150), _now), _now);
        auction.PlaceBid(new Bid(auction.Id, _bidderId, new Money(300), _now), _now);
        auction.End(_now);
        var endedEvent = auction.DomainEvents.OfType<AuctionEndedEvent>().First();
        endedEvent.WinnerId.Should().Be(_bidderId);
        endedEvent.WinningAmount.Should().Be(300);
    }
}