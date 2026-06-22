using System;
using System.Threading.Tasks;
using FluentAssertions;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.ValueObjects;
using Xunit;

namespace LilyMarket.Tests.Integration;

public class AuctionLifecycleTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public AuctionLifecycleTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FullLifecycle_CreateBidEnd_WinnerCorrect()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var sellerId = Guid.NewGuid();
        var bidder1Id = Guid.NewGuid();
        var bidder2Id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var auction = new Auction(
            sellerId,
            "Lifecycle Test Item",
            "Testing complete auction lifecycle",
            "Furniture",
            "Good",
            "https://example.com/furniture.jpg",
            new Money(100),
            new Money(10),
            new Money(500),
            now.AddDays(7),
            now);

        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act - добавляем ставки и сохраняем каждую отдельно
        var bid1 = new Bid(auction.Id, bidder1Id, new Money(150), now);
        auction.PlaceBid(bid1, now);
        context.Bids.Add(bid1);
        await context.SaveChangesAsync();

        var bid2 = new Bid(auction.Id, bidder2Id, new Money(200), now);
        auction.PlaceBid(bid2, now);
        context.Bids.Add(bid2);
        await context.SaveChangesAsync();

        var bid3 = new Bid(auction.Id, bidder1Id, new Money(250), now);
        auction.PlaceBid(bid3, now);
        context.Bids.Add(bid3);
        await context.SaveChangesAsync();

        // Завершаем аукцион
        auction.End(now.AddDays(7));
        await context.SaveChangesAsync();

        // Assert
        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.CurrentHighestBid.Should().Be(250);
        auction.CurrentHighestBidderId.Should().Be(bidder1Id);
        auction.Bids.Should().HaveCount(3);
    }

    [Fact]
    public async Task Auction_WithoutBids_EndsCorrectly()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var auction = new Auction(
            sellerId,
            "No Bids Item",
            "Testing auction with no bids",
            "Art",
            "New",
            "https://example.com/art.jpg",
            new Money(100),
            new Money(10),
            null,
            now.AddDays(7),
            now);

        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act
        auction.End(now.AddDays(7));
        await context.SaveChangesAsync();

        // Assert
        auction.Status.Should().Be(AuctionStatus.Ended);
        auction.CurrentHighestBid.Should().BeNull();
        auction.CurrentHighestBidderId.Should().BeNull();
        auction.Bids.Should().BeEmpty();
    }

    [Fact]
    public async Task Auction_BuyNow_ClosesImmediately()
    {
        // Arrange
        using var context = _fixture.CreateContext();

        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var auction = new Auction(
            sellerId,
            "BuyNow Item",
            "Testing buy now immediate close",
            "Clothing",
            "Like New",
            "https://example.com/clothing.jpg",
            new Money(100),
            new Money(10),
            new Money(300),
            now.AddDays(7),
            now);

        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act - добавляем bid в контекст явно
        var bid = new Bid(auction.Id, bidderId, new Money(300), now);
        auction.PlaceBid(bid, now);
        context.Bids.Add(bid);
        await context.SaveChangesAsync();

        // Assert
        auction.Status.Should().Be(AuctionStatus.Sold);
        auction.CurrentHighestBid.Should().Be(300);
        auction.CurrentHighestBidderId.Should().Be(bidderId);
    }
}