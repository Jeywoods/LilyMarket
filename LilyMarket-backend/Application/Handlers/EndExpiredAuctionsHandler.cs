using LilyMarket.Application.DTO.Notifications;
using LilyMarket.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class EndExpiredAuctionsHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<EndExpiredAuctionsHandler> _logger;

    public EndExpiredAuctionsHandler(
        IAuctionRepository auctionRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<EndExpiredAuctionsHandler> logger)
    {
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var expiredAuctions = await _auctionRepository.GetExpiredActiveAuctionsAsync(now, ct);

        foreach (var auction in expiredAuctions)
        {
            auction.End(now);
            _auctionRepository.Update(auction);

            foreach (var domainEvent in auction.DomainEvents)
            {
                switch (domainEvent)
                {
                    case Domain.Events.AuctionEndedEvent ended:
                        await _notificationService.NotifyAuctionEndedAsync(
                            auction.Id,
                            new AuctionEndedNotification
                            {
                                AuctionId = ended.AuctionId,
                                WinnerId = ended.WinnerId,
                                WinningAmount = ended.WinningAmount,
                                EndedAt = now
                            });

                        _logger.LogInformation(
                            "Auction {AuctionId} ended. Winner: {WinnerId}, Amount: {Amount}",
                            auction.Id, ended.WinnerId, ended.WinningAmount);
                        break;

                    case Domain.Events.AuctionEndedNoWinnerEvent noWinner:
                        await _notificationService.NotifySellerNoWinnerAsync(
                            noWinner.SellerId, auction.Id);

                        _logger.LogInformation(
                            "Auction {AuctionId} ended with no bids", auction.Id);
                        break;
                }
            }

            auction.ClearDomainEvents();
        }

        if (expiredAuctions.Any())
            await _unitOfWork.SaveChangesAsync(ct);
    }
}