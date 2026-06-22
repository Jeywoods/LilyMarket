using FluentAssertions;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Handlers;

public class CancelAuctionHandlerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<CancelAuctionHandler>> _loggerMock = new();
    private readonly CancelAuctionHandler _handler;

    public CancelAuctionHandlerTests()
    {
        _handler = new CancelAuctionHandler(
            _auctionRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoBids_CancelsSuccessfully()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsAsync(auction.Id, default))
            .ReturnsAsync(auction);

        await _handler.Handle(auction.Id, sellerId);

        auction.Status.Should().Be(AuctionStatus.Canceled);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_WithBids_ThrowsUnauthorizedOperationException()
    {
        var sellerId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        auction.PlaceBid(new Bid(auction.Id, bidderId, new Money(150), now), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var act = () => _handler.Handle(auction.Id, sellerId);

        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task Handle_NotSeller_ThrowsUnauthorizedOperationException()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var act = () => _handler.Handle(auction.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedOperationException>();
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsAuctionNotFoundException()
    {
        _auctionRepoMock.Setup(x => x.GetByIdWithBidsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Auction?)null);

        var act = () => _handler.Handle(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }
}