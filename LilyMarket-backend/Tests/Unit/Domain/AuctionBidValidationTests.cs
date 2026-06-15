using FluentAssertions;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.Events;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;
using Xunit;

namespace LilyMarket.Tests.Unit.Domain;

public class AuctionBidValidationTests
{
    private readonly Guid _sellerId = Guid.NewGuid();
    private readonly Guid _bidderId = Guid.NewGuid();
    private readonly DateTime _now;

    public AuctionBidValidationTests()
    {
        _now = DateTime.UtcNow;
    }

    private Auction CreateActiveAuction(decimal startingPrice = 100, decimal minIncrement = 10, decimal? buyNowPrice = null)
    {
        return new Auction(
            _sellerId,
            "Test Item",
            "Test Description",
            "Tech",
            "Good",
            "https://example.com/photo.jpg",
            new Money(startingPrice),
            new Money(minIncrement),
            buyNowPrice is not null ? new Money(buyNowPrice.Value) : null,
            _now.AddDays(7),
            _now);
    }

    [Fact]
    public void PlaceBid_ValidBid_AcceptsAndUpdatesHighestBid()
    {
        var auction = CreateActiveAuction();
        var bid = new Bid(auction.Id, _bidderId, new Money(150), _now);
        auction.PlaceBid(bid, _now);
        auction.CurrentHighestBid.Should().Be(150);
        auction.CurrentHighestBidderId.Should().Be(_bidderId);
        auction.Bids.Should().HaveCount(1);
    }

    [Fact]
    public void PlaceBid_ExactlyMinimumIncrement_Accepts()
    {
        var auction = CreateActiveAuction(startingPrice: 100, minIncrement: 10);
        var bid = new Bid(auction.Id, _bidderId, new Money(110), _now);
        auction.PlaceBid(bid, _now);
        auction.CurrentHighestBid.Should().Be(110);
    }

    [Fact]
    public void PlaceBid_BelowMinimumIncrement_ThrowsBidTooLowException()
    {
        var auction = CreateActiveAuction(startingPrice: 100, minIncrement: 10);
        var bid = new Bid(auction.Id, _bidderId, new Money(105), _now);
        var act = () => auction.PlaceBid(bid, _now);
        act.Should().Throw<BidTooLowException>().WithMessage("*110*");
    }

    [Fact]
    public void PlaceBid_EqualStartingPrice_ThrowsBidTooLowException()
    {
        var auction = CreateActiveAuction(startingPrice: 100, minIncrement: 10);
        var bid = new Bid(auction.Id, _bidderId, new Money(100), _now);
        var act = () => auction.PlaceBid(bid, _now);
        act.Should().Throw<BidTooLowException>();
    }

    [Fact]
    public void PlaceBid_ZeroAmount_ThrowsArgumentException()
    {
        var auction = CreateActiveAuction();
        var act = () => new Bid(auction.Id, _bidderId, new Money(0), _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PlaceBid_NegativeAmount_ThrowsArgumentException()
    {
        var auction = CreateActiveAuction();
        var act = () => new Bid(auction.Id, _bidderId, new Money(-50), _now);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PlaceBid_AfterEndTime_ThrowsAuctionExpiredException()
    {
        var auction = CreateActiveAuction();
        var lateTime = _now.AddDays(8);
        var bid = new Bid(auction.Id, _bidderId, new Money(150), lateTime);
        var act = () => auction.PlaceBid(bid, lateTime);
        act.Should().Throw<AuctionExpiredException>();
    }

    [Fact]
    public void PlaceBid_ExactEndTime_ThrowsAuctionExpiredException()
    {
        var endTime = _now.AddHours(1);
        var auction = new Auction(_sellerId, "T", "D", "Tech", "Good", "url", new Money(100), new Money(10), null, endTime, _now);
        var bid = new Bid(auction.Id, _bidderId, new Money(150), endTime);
        var act = () => auction.PlaceBid(bid, endTime);
        act.Should().Throw<AuctionExpiredException>();
    }

    [Fact]
    public void PlaceBid_BySeller_ThrowsSellerCannotBidException()
    {
        var auction = CreateActiveAuction();
        var bid = new Bid(auction.Id, _sellerId, new Money(150), _now);
        var act = () => auction.PlaceBid(bid, _now);
        act.Should().Throw<SellerCannotBidException>();
    }

    [Fact]
    public void PlaceBid_ReachesBuyNowPrice_ClosesAuctionImmediately()
    {
        var auction = CreateActiveAuction(startingPrice: 100, minIncrement: 10, buyNowPrice: 200);
        var bid = new Bid(auction.Id, _bidderId, new Money(200), _now);
        auction.PlaceBid(bid, _now);
        auction.Status.Should().Be(AuctionStatus.Sold);
        auction.DomainEvents.Should().Contain(e => e is BuyNowTriggeredEvent);
    }

    [Fact]
    public void PlaceBid_ExceedsBuyNowPrice_ClosesAuction()
    {
        var auction = CreateActiveAuction(startingPrice: 100, minIncrement: 10, buyNowPrice: 200);
        var bid = new Bid(auction.Id, _bidderId, new Money(999), _now);
        auction.PlaceBid(bid, _now);
        auction.Status.Should().Be(AuctionStatus.Sold);
    }

    [Fact]
    public void PlaceBid_BelowBuyNowPrice_KeepsActive()
    {
        var auction = CreateActiveAuction(startingPrice: 100, minIncrement: 10, buyNowPrice: 200);
        var bid = new Bid(auction.Id, _bidderId, new Money(150), _now);
        auction.PlaceBid(bid, _now);
        auction.Status.Should().Be(AuctionStatus.Active);
    }

    [Fact]
    public void PlaceBid_OnEndedAuction_ThrowsAuctionExpiredException()
    {
        var auction = CreateActiveAuction();
        auction.End(_now);
        var bid = new Bid(auction.Id, _bidderId, new Money(150), _now);
        var act = () => auction.PlaceBid(bid, _now);
        act.Should().Throw<AuctionExpiredException>();
    }

    [Fact]
    public void PlaceBid_OnCanceledAuction_ThrowsAuctionExpiredException()
    {
        var auction = CreateActiveAuction();
        auction.Cancel(_sellerId);
        var bid = new Bid(auction.Id, _bidderId, new Money(150), _now);
        var act = () => auction.PlaceBid(bid, _now);
        act.Should().Throw<AuctionExpiredException>();
    }

    [Fact]
    public void PlaceBid_OutbidsPreviousBidder_GeneratesOutbidEvent()
    {
        var auction = CreateActiveAuction();
        var otherBidderId = Guid.NewGuid();
        var firstBid = new Bid(auction.Id, otherBidderId, new Money(150), _now);
        auction.PlaceBid(firstBid, _now);
        var secondBid = new Bid(auction.Id, _bidderId, new Money(200), _now);
        auction.PlaceBid(secondBid, _now);
        auction.DomainEvents.Should().Contain(e => e is BidOutbidEvent);
    }

    [Fact]
    public void PlaceBid_SameBidderRaisesOwnBid_NoOutbidEvent()
    {
        var auction = CreateActiveAuction();
        var firstBid = new Bid(auction.Id, _bidderId, new Money(150), _now);
        auction.PlaceBid(firstBid, _now);
        auction.ClearDomainEvents();
        var secondBid = new Bid(auction.Id, _bidderId, new Money(200), _now);
        auction.PlaceBid(secondBid, _now);
        auction.DomainEvents.Should().NotContain(e => e is BidOutbidEvent);
    }
}