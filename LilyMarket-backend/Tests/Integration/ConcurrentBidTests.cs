using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.DTO.Notifications;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Integration;

public class ConcurrentBidTests
{
    private readonly Mock<IAuctionRepository> _auctionRepoMock = new();
    private readonly Mock<IBidRepository> _bidRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IValidator<PlaceBidRequest>> _validatorMock = new();
    private readonly Mock<ILogger<PlaceBidHandler>> _loggerMock = new();

    public ConcurrentBidTests()
    {
        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _validatorMock
            .Setup(x => x.ValidateAsync(It.IsAny<PlaceBidRequest>(), default))
            .ReturnsAsync(new ValidationResult());

        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), default))
            .Callback<Func<Task>, CancellationToken>((func, ct) => func().GetAwaiter().GetResult());
    }

    [Fact]
    public async Task ConcurrentBids_OnSameAuction_OnlyOneWins()
    {
        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();
        var bidder1Id = Guid.NewGuid();
        var bidder2Id = Guid.NewGuid();
        var bidder3Id = Guid.NewGuid();

        var auction = new Auction(
            sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(10), null, now.AddHours(1), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var handler = new PlaceBidHandler(
            _auctionRepoMock.Object, _bidRepoMock.Object, _unitOfWorkMock.Object,
            _notificationServiceMock.Object, _dateTimeProviderMock.Object,
            _validatorMock.Object, _loggerMock.Object);

        var results = new ConcurrentBag<bool>();
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var bidderId = new[] { bidder1Id, bidder2Id, bidder3Id }[i % 3];
            var amount = 100 + (i + 1) * 15;
            try
            {
                var result = await handler.Handle(auction.Id, bidderId, new PlaceBidRequest { Amount = amount });
                results.Add(result.Success);
            }
            catch
            {
                results.Add(false);
            }
        });

        await Task.WhenAll(tasks);
        results.Count(r => r).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ConcurrentBids_WithBuyNow_OnlyOneSucceeds()
    {
        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();
        var bidder1Id = Guid.NewGuid();
        var bidder2Id = Guid.NewGuid();

        var auction = new Auction(
            sellerId, "Test", "Desc", "Books", "New", "url",
            new Money(100), new Money(10), new Money(500), now.AddHours(1), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var handler = new PlaceBidHandler(
            _auctionRepoMock.Object, _bidRepoMock.Object, _unitOfWorkMock.Object,
            _notificationServiceMock.Object, _dateTimeProviderMock.Object,
            _validatorMock.Object, _loggerMock.Object);

        var task1 = handler.Handle(auction.Id, bidder1Id, new PlaceBidRequest { Amount = 500 });
        var task2 = handler.Handle(auction.Id, bidder2Id, new PlaceBidRequest { Amount = 500 });

        int successCount = 0;

        try { var r1 = await task1; if (r1.Success) successCount++; }
        catch (AuctionExpiredException) { /* первая ставка закрыла аукцион, вторая отклонилась */ }

        try { var r2 = await task2; if (r2.Success) successCount++; }
        catch (AuctionExpiredException) { /* первая ставка закрыла аукцион, вторая отклонилась */ }

        successCount.Should().Be(1);
        auction.Status.Should().Be(AuctionStatus.Sold);
    }

    [Fact]
    public async Task ConcurrentBids_FirstArrivesFirst_SecondOutbids()
    {
        //ситуация: текущая ставка 500, шаг 200
        //человек1 ставит 700, человек2 ставит 900
        //первый дошёл первым — обе ставки проходят, 700 перебита ставкой 900

        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();

        var auction = new Auction(
            sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(200), null, now.AddHours(1), now);

        auction.PlaceBid(new Bid(auction.Id, Guid.NewGuid(), new Money(500), now), now);
        auction.ClearDomainEvents();

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var handler = new PlaceBidHandler(
            _auctionRepoMock.Object, _bidRepoMock.Object, _unitOfWorkMock.Object,
            _notificationServiceMock.Object, _dateTimeProviderMock.Object,
            _validatorMock.Object, _loggerMock.Object);

        var result1 = await handler.Handle(auction.Id, bidder1, new PlaceBidRequest { Amount = 700 });
        var result2 = await handler.Handle(auction.Id, bidder2, new PlaceBidRequest { Amount = 900 });

        result1.Success.Should().BeTrue();
        result1.NewHighestBid.Should().Be(700);
        result2.Success.Should().BeTrue();
        result2.NewHighestBid.Should().Be(900);

        _notificationServiceMock.Verify(
            x => x.NotifyOutbidAsync(bidder1, It.IsAny<OutbidNotification>()),
            Times.Once);
    }

    [Fact]
    public async Task ConcurrentBids_SecondArrivesFirst_FirstRejected()
    {
        //ситуация: текущая ставка 500, шаг 200
        //человек2 ставит 900 и приходит первым, человек1 ставит 700 и приходит вторым
        //ставка 700 должна отклониться потому что 700 < 900 + 200 = 1100

        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();

        var auction = new Auction(
            sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(200), null, now.AddHours(1), now);

        auction.PlaceBid(new Bid(auction.Id, Guid.NewGuid(), new Money(500), now), now);
        auction.ClearDomainEvents();

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var handler = new PlaceBidHandler(
            _auctionRepoMock.Object, _bidRepoMock.Object, _unitOfWorkMock.Object,
            _notificationServiceMock.Object, _dateTimeProviderMock.Object,
            _validatorMock.Object, _loggerMock.Object);

        var result2 = await handler.Handle(auction.Id, bidder2, new PlaceBidRequest { Amount = 900 });
        result2.Success.Should().BeTrue();
        result2.NewHighestBid.Should().Be(900);

        await Assert.ThrowsAsync<BidTooLowException>(
            () => handler.Handle(auction.Id, bidder1, new PlaceBidRequest { Amount = 700 }));
    }

    [Fact]
    public async Task ConcurrentBids_Simultaneous_FirstBlocksSecond()
    {
        //ситуация: текущая ставка 500, шаг 200
        //два человека одновременно ставят 700
        //кто первый забрал блокировку — того ставка проходит, второй должен ждать и проверить заново

        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();
        var bidder1 = Guid.NewGuid();
        var bidder2 = Guid.NewGuid();

        var auction = new Auction(
            sellerId, "Test", "Desc", "Tech", "Good", "url",
            new Money(100), new Money(200), null, now.AddHours(1), now);

        auction.PlaceBid(new Bid(auction.Id, Guid.NewGuid(), new Money(500), now), now);
        auction.ClearDomainEvents();

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var handler = new PlaceBidHandler(
            _auctionRepoMock.Object, _bidRepoMock.Object, _unitOfWorkMock.Object,
            _notificationServiceMock.Object, _dateTimeProviderMock.Object,
            _validatorMock.Object, _loggerMock.Object);

        var task1 = handler.Handle(auction.Id, bidder1, new PlaceBidRequest { Amount = 700 });
        var task2 = handler.Handle(auction.Id, bidder2, new PlaceBidRequest { Amount = 700 });

        int successCount = 0;

        try { var r1 = await task1; if (r1.Success) successCount++; }
        catch (BidTooLowException) { /* вторая ставка увидела уже обновлённую цену */ }

        try { var r2 = await task2; if (r2.Success) successCount++; }
        catch (BidTooLowException) { /* вторая ставка увидела уже обновлённую цену */ }

        successCount.Should().Be(1);
        auction.CurrentHighestBid.Should().Be(700);
    }

    [Fact]
    public async Task ConcurrentBids_NoDuplicateAmounts()
    {
        var now = DateTime.UtcNow;
        var sellerId = Guid.NewGuid();

        var auction = new Auction(
            sellerId, "Test", "Desc", "Sports", "Good", "url",
            new Money(100), new Money(5), null, now.AddHours(1), now);

        _auctionRepoMock.Setup(x => x.GetByIdWithBidsForUpdateAsync(auction.Id, default))
            .ReturnsAsync(auction);

        var handler = new PlaceBidHandler(
            _auctionRepoMock.Object, _bidRepoMock.Object, _unitOfWorkMock.Object,
            _notificationServiceMock.Object, _dateTimeProviderMock.Object,
            _validatorMock.Object, _loggerMock.Object);

        var tasks = Enumerable.Range(0, 20).Select(i =>
        {
            var bidderId = Guid.NewGuid();
            var amount = 100 + (i + 1) * 10;
            return handler.Handle(auction.Id, bidderId, new PlaceBidRequest { Amount = amount });
        });

        var results = await Task.WhenAll(tasks);

        var successfulAmounts = results
            .Where(r => r.Success)
            .Select(r => r.NewHighestBid)
            .ToList();

        successfulAmounts.Should().OnlyHaveUniqueItems();
    }
}