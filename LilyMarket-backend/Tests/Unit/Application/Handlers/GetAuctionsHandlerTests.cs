using FluentAssertions;
using LilyMarket.Application.Common;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.ValueObjects;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Handlers;

public class GetAuctionsHandlerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly GetAuctionsHandler _handler;

    public GetAuctionsHandlerTests()
    {
        _handler = new GetAuctionsHandler(_auctionRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var now = DateTime.UtcNow;
        var auctions = Enumerable.Range(0, 5).Select(i => new Auction(
            Guid.NewGuid(), $"Title {i}", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now)).ToList();

        _auctionRepoMock.Setup(x => x.GetPagedAsync(1, 2, default))
            .ReturnsAsync(new PagedResult<Auction>(auctions.Take(2), 5, 1, 2));

        var result = await _handler.Handle(1, 2);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoAuctions_ReturnsEmpty()
    {
        _auctionRepoMock.Setup(x => x.GetPagedAsync(1, 20, default))
            .ReturnsAsync(new PagedResult<Auction>(Array.Empty<Auction>(), 0, 1, 20));

        var result = await _handler.Handle(1, 20);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectBidCount()
    {
        var now = DateTime.UtcNow;
        var auction = new Auction(
            Guid.NewGuid(), "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        auction.PlaceBid(new Bid(auction.Id, Guid.NewGuid(), new Money(150), now), now);
        auction.PlaceBid(new Bid(auction.Id, Guid.NewGuid(), new Money(200), now), now);

        _auctionRepoMock.Setup(x => x.GetPagedAsync(1, 20, default))
            .ReturnsAsync(new PagedResult<Auction>(new[] { auction }, 1, 1, 20));

        var result = await _handler.Handle(1, 20);

        result.Items.First().BidCount.Should().Be(2);
    }
}