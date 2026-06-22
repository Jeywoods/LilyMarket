using FluentAssertions;
using LilyMarket.Application.DTO.Notifications;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Handlers;

public class EndExpiredAuctionsHandlerTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<ILogger<EndExpiredAuctionsHandler>> _loggerMock = new();
    private readonly EndExpiredAuctionsHandler _handler;

    public EndExpiredAuctionsHandlerTests()
    {
        _handler = new EndExpiredAuctionsHandler(
            _auctionRepoMock.Object,
            _unitOfWorkMock.Object,
            _notificationServiceMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ExpiredWithBids_EndsAndNotifiesWinner()
    {
        // Arrange
        var now = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var sellerId = Guid.NewGuid();
        var winnerId = Guid.NewGuid();

        // EndTime в прошлом относительно now
        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null,
            now.AddHours(-2), // EndTime — 2 часа назад
            now.AddDays(-2)); // StartedAt — 2 дня назад

        auction.PlaceBid(new Bid(auction.Id, winnerId, new Money(150), now.AddDays(-1)), now.AddDays(-1));

        _auctionRepoMock.Setup(x => x.GetExpiredActiveAuctionsAsync(now, default))
            .ReturnsAsync(new[] { auction });

        // Act
        await _handler.Handle();

        // Assert
        auction.Status.Should().Be(AuctionStatus.Ended);
        _notificationServiceMock.Verify(
            x => x.NotifyAuctionEndedAsync(
                auction.Id,
                It.Is<AuctionEndedNotification>(n => n.WinnerId == winnerId)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredWithoutBids_NotifiesSeller()
    {
        // Arrange
        var now = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(now);

        var sellerId = Guid.NewGuid();

        var auction = new Auction(sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null,
            now.AddHours(-2), // EndTime — 2 часа назад
            now.AddDays(-2));

        _auctionRepoMock.Setup(x => x.GetExpiredActiveAuctionsAsync(now, default))
            .ReturnsAsync(new[] { auction });

        // Act
        await _handler.Handle();

        // Assert
        auction.Status.Should().Be(AuctionStatus.Ended);
        _notificationServiceMock.Verify(
            x => x.NotifySellerNoWinnerAsync(sellerId, auction.Id),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoExpiredAuctions_DoesNothing()
    {
        _auctionRepoMock.Setup(x => x.GetExpiredActiveAuctionsAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(Array.Empty<Auction>());

        await _handler.Handle();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }
}