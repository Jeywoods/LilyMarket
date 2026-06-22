using FluentAssertions;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Handlers;

public class UpdateAuctionHandlerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<UpdateAuctionHandler>> _loggerMock = new();
    private readonly UpdateAuctionHandler _handler;

    public UpdateAuctionHandlerTests()
    {
        _handler = new UpdateAuctionHandler(
            _auctionRepoMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesFields()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Old Title", "Old Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), new Money(500), now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var request = new UpdateAuctionRequest
        {
            Title = "New Title",
            Description = "New Desc",
            BuyNowPrice = 600
        };

        var result = await _handler.Handle(auction.Id, sellerId, request);

        result.Title.Should().Be("New Title");
        result.Description.Should().Be("New Desc");
        result.BuyNowPrice.Should().Be(600);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_NotSeller_ThrowsUnauthorizedOperationException()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var act = () => _handler.Handle(auction.Id, Guid.NewGuid(), new UpdateAuctionRequest());

        await act.Should().ThrowAsync<UnauthorizedOperationException>()
            .WithMessage("*seller*");
    }

    [Fact]
    public async Task Handle_WithBids_ThrowsUnauthorizedOperationException()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Title", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        auction.PlaceBid(new Bid(auction.Id, Guid.NewGuid(), new Money(150), now), now);

        _auctionRepoMock.Setup(x => x.GetByIdAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var act = () => _handler.Handle(auction.Id, sellerId, new UpdateAuctionRequest());

        await act.Should().ThrowAsync<UnauthorizedOperationException>()
            .WithMessage("*bids*");
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsAuctionNotFoundException()
    {
        _auctionRepoMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Auction?)null);

        var act = () => _handler.Handle(Guid.NewGuid(), Guid.NewGuid(), new UpdateAuctionRequest());

        await act.Should().ThrowAsync<AuctionNotFoundException>();
    }

    [Fact]
    public async Task Handle_PartialUpdate_UpdatesOnlyProvided()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Original", "Original Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var request = new UpdateAuctionRequest { Title = "Updated" };

        var result = await _handler.Handle(auction.Id, sellerId, request);

        result.Title.Should().Be("Updated");
        result.Description.Should().Be("Original Desc");
    }
}