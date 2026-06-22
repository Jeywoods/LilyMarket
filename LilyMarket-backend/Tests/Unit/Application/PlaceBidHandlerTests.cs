using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.DTO.Notifications;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application;

public class PlaceBidHandlerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly Mock<IBidRepository> _bidRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IValidator<PlaceBidRequest>> _validatorMock = new();
    private readonly Mock<ILogger<PlaceBidHandler>> _loggerMock = new();

    private readonly PlaceBidHandler _handler;

    public PlaceBidHandlerTests()
    {
        _handler = new PlaceBidHandler(
            _auctionRepoMock.Object,
            _bidRepoMock.Object,
            _unitOfWorkMock.Object,
            _notificationServiceMock.Object,
            _dateTimeProviderMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<PlaceBidRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, CancellationToken>((func, ct) => func().GetAwaiter().GetResult());
    }

    private Auction SetupAuction(decimal? buyNowPrice = null)
    {
        var now = DateTime.UtcNow;
        var auction = new Auction(
            Guid.NewGuid(), "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10),
            buyNowPrice.HasValue ? new Money(buyNowPrice.Value) : null,
            now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        return auction;
    }

    [Fact]
    public async Task Handle_ValidBid_SavesAndNotifies()
    {
        var auction = SetupAuction();
        var bidderId = Guid.NewGuid();

        var result = await _handler.Handle(auction.Id, bidderId, new PlaceBidRequest { Amount = 150 });

        result.Success.Should().BeTrue();
        result.NewHighestBid.Should().Be(150);
        _bidRepoMock.Verify(x => x.AddAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>()), Times.Once);
        _auctionRepoMock.Verify(x => x.Update(auction), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(
            x => x.NotifyBidPlacedAsync(
                auction.Id,
                It.Is<BidPlacedNotification>(n =>
                    n.AuctionId == auction.Id &&
                    n.NewHighestBid == 150 &&
                    n.BidderId == bidderId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidBid_ReturnsCorrectNewHighestBid()
    {
        var auction = SetupAuction();
        var result = await _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 200 });
        result.NewHighestBid.Should().Be(200);
    }

    [Fact]
    public async Task Handle_OutbidsPreviousBidder_NotifiesOutbid()
    {
        var auction = SetupAuction();
        var firstBidderId = Guid.NewGuid();
        var secondBidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        auction.PlaceBid(new Bid(auction.Id, firstBidderId, new Money(150), now), now);
        auction.ClearDomainEvents();

        var result = await _handler.Handle(auction.Id, secondBidderId, new PlaceBidRequest { Amount = 200 });

        result.Success.Should().BeTrue();
        _notificationServiceMock.Verify(
            x => x.NotifyOutbidAsync(
                firstBidderId,
                It.Is<OutbidNotification>(n =>
                    n.AuctionId == auction.Id &&
                    n.NewHighestBid == 200)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SameBidderRaisesBid_NoOutbidNotification()
    {
        var auction = SetupAuction();
        var bidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        auction.PlaceBid(new Bid(auction.Id, bidderId, new Money(150), now), now);
        auction.ClearDomainEvents();

        await _handler.Handle(auction.Id, bidderId, new PlaceBidRequest { Amount = 200 });

        _notificationServiceMock.Verify(
            x => x.NotifyOutbidAsync(It.IsAny<Guid>(), It.IsAny<OutbidNotification>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BuyNowReached_ClosesAuctionAndNotifies()
    {
        var auction = SetupAuction(buyNowPrice: 300);
        var bidderId = Guid.NewGuid();

        var result = await _handler.Handle(auction.Id, bidderId, new PlaceBidRequest { Amount = 300 });

        result.Success.Should().BeTrue();
        result.NewHighestBid.Should().Be(300);
        _notificationServiceMock.Verify(
            x => x.NotifyAuctionEndedAsync(
                auction.Id,
                It.Is<AuctionEndedNotification>(n =>
                    n.WinnerId == bidderId &&
                    n.WinningAmount == 300)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_BuyNowExceeded_ClosesAuction()
    {
        var auction = SetupAuction(buyNowPrice: 300);
        var result = await _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 500 });

        result.Success.Should().BeTrue();
        _notificationServiceMock.Verify(
            x => x.NotifyAuctionEndedAsync(auction.Id, It.IsAny<AuctionEndedNotification>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AuctionEnded_ThrowsAuctionExpiredException()
    {
        var auction = SetupAuction();
        auction.End(DateTime.UtcNow);

        await Assert.ThrowsAsync<AuctionExpiredException>(
            () => _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 150 }));
    }

    [Fact]
    public async Task Handle_AuctionCanceled_ThrowsAuctionExpiredException()
    {
        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();
        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);
        auction.Cancel(sellerId);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        await Assert.ThrowsAsync<AuctionExpiredException>(
            () => _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 150 }));
    }

    [Fact]
    public async Task Handle_SellerBids_ThrowsSellerCannotBidException()
    {
        var sellerId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        await Assert.ThrowsAsync<SellerCannotBidException>(
            () => _handler.Handle(auction.Id, sellerId, new PlaceBidRequest { Amount = 150 }));
    }

    [Fact]
    public async Task Handle_BidTooLow_ThrowsBidTooLowException()
    {
        var auction = SetupAuction();

        await Assert.ThrowsAsync<BidTooLowException>(
            () => _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 105 }));
    }

    [Fact]
    public async Task Handle_BidTooLowAfterFirstBid_ThrowsBidTooLowException()
    {
        var auction = SetupAuction();
        var firstBidderId = Guid.NewGuid();
        auction.PlaceBid(new Bid(auction.Id, firstBidderId, new Money(150), DateTime.UtcNow), DateTime.UtcNow);

        await Assert.ThrowsAsync<BidTooLowException>(
            () => _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 155 }));
    }

    [Fact]
    public async Task Handle_ExactlyMinimumIncrement_Accepts()
    {
        var auction = SetupAuction();
        var result = await _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 110 });

        result.Success.Should().BeTrue();
        result.NewHighestBid.Should().Be(110);
    }

    [Fact]
    public async Task Handle_AuctionExpiredByServerTime_ThrowsAuctionExpiredException()
    {
        var now = DateTime.UtcNow;
        var lateTime = now.AddDays(8);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(lateTime);

        var auction = new Auction(Guid.NewGuid(), "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddDays(7), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        await Assert.ThrowsAsync<AuctionExpiredException>(
            () => _handler.Handle(auction.Id, Guid.NewGuid(), new PlaceBidRequest { Amount = 150 }));
    }

    [Fact]
    public async Task Handle_AuctionNotFound_ThrowsAuctionNotFoundException()
    {
        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Auction?)null);

        await Assert.ThrowsAsync<AuctionNotFoundException>(
            () => _handler.Handle(Guid.NewGuid(), Guid.NewGuid(), new PlaceBidRequest { Amount = 150 }));
    }

    [Fact]
    public async Task Handle_MultipleBids_DifferentBidders_CorrectNotifications()
    {
        var auction = SetupAuction();
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();
        var bidder3 = Guid.NewGuid();

        await _handler.Handle(auction.Id, bidder1, new PlaceBidRequest { Amount = 150 });
        await _handler.Handle(auction.Id, bidder2, new PlaceBidRequest { Amount = 200 });
        await _handler.Handle(auction.Id, bidder3, new PlaceBidRequest { Amount = 250 });

        _notificationServiceMock.Verify(
            x => x.NotifyBidPlacedAsync(auction.Id, It.IsAny<BidPlacedNotification>()),
            Times.Exactly(3));

        _notificationServiceMock.Verify(
            x => x.NotifyOutbidAsync(bidder1, It.IsAny<OutbidNotification>()),
            Times.Once);

        _notificationServiceMock.Verify(
            x => x.NotifyOutbidAsync(bidder2, It.IsAny<OutbidNotification>()),
            Times.Once);

        _notificationServiceMock.Verify(
            x => x.NotifyOutbidAsync(bidder3, It.IsAny<OutbidNotification>()),
            Times.Never);
    }
}