using FluentAssertions;
using FluentValidation;
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
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    [Fact]
    public async Task Handle_ValidBid_SavesAndNotifies()
    {
        var auctionId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var auction = new Auction(
            Guid.NewGuid(),
            "Test",
            "Desc",
            "Tech",
            "Good",
            "https://example.com/photo.jpg",
            new Money(100),
            new Money(10),
            null,
            now.AddDays(7),
            now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsAsync(auctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        _unitOfWorkMock.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, CancellationToken>((func, ct) => func().GetAwaiter().GetResult());

        var request = new PlaceBidRequest { Amount = 150 };

        var result = await _handler.Handle(auctionId, bidderId, request);

        result.Success.Should().BeTrue();
        _bidRepoMock.Verify(x => x.AddAsync(It.IsAny<Bid>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(
            x => x.NotifyBidPlacedAsync(
                It.IsAny<Guid>(),
                It.IsAny<BidPlacedNotification>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidBid_DoesNotSave()
    {
        var auctionId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var auction = new Auction(
            Guid.NewGuid(),
            "Test",
            "Desc",
            "Tech",
            "Good",
            "https://example.com/photo.jpg",
            new Money(100),
            new Money(10),
            null,
            now.AddDays(7),
            now);

        auction.End(now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsAsync(auctionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(auction);

        _unitOfWorkMock.Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Callback<Func<Task>, CancellationToken>((func, ct) => func().GetAwaiter().GetResult());

        var request = new PlaceBidRequest { Amount = 150 };

        await Assert.ThrowsAsync<AuctionExpiredException>(
            () => _handler.Handle(auctionId, bidderId, request));
    }
}