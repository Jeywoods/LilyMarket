using FluentValidation;
using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.DTO.Notifications;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class PlaceBidHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IBidRepository _bidRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<PlaceBidRequest> _validator;
    private readonly ILogger<PlaceBidHandler> _logger;

    public PlaceBidHandler(
        IAuctionRepository auctionRepository,
        IBidRepository bidRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        IValidator<PlaceBidRequest> validator,
        ILogger<PlaceBidHandler> logger)
    {
        _auctionRepository = auctionRepository;
        _bidRepository = bidRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<BidResultDto> Handle(Guid auctionId, Guid bidderId, PlaceBidRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, ct);

            if (auction is null)
                throw new AuctionNotFoundException(auctionId);

            var bid = new Bid(
                auctionId,
                bidderId,
                new Domain.ValueObjects.Money(request.Amount),
                _dateTimeProvider.UtcNow);

            auction.PlaceBid(bid, _dateTimeProvider.UtcNow);

            await _bidRepository.AddAsync(bid, ct);
            _auctionRepository.Update(auction);
            await _unitOfWork.SaveChangesAsync(ct);

            await DispatchEvents(auction);

            _logger.LogInformation(
                "Bid of {Amount} placed on auction {AuctionId} by {BidderId}, new highest: {NewHighest}",
                request.Amount, auctionId, bidderId, auction.CurrentHighestBid);
        }, ct);

        return new BidResultDto
        {
            Success = true,
            NewHighestBid = request.Amount,
            Message = "Bid placed successfully"
        };
    }

    private async Task DispatchEvents(Auction auction)
    {
        foreach (var domainEvent in auction.DomainEvents)
        {
            switch (domainEvent)
            {
                case Domain.Events.BidPlacedEvent bidPlaced:
                    await _notificationService.NotifyBidPlacedAsync(
                        bidPlaced.AuctionId,
                        new BidPlacedNotification
                        {
                            AuctionId = bidPlaced.AuctionId,
                            NewHighestBid = bidPlaced.NewHighestBid,
                            BidCount = auction.Bids.Count,
                            BidderId = bidPlaced.BidderId
                        });
                    break;

                case Domain.Events.BidOutbidEvent outbid:
                    await _notificationService.NotifyOutbidAsync(
                        outbid.PreviousHighestBidderId,
                        new OutbidNotification
                        {
                            AuctionId = outbid.AuctionId,
                            YourPreviousBid = auction.Bids
                                .Where(b => b.BidderId == outbid.PreviousHighestBidderId)
                                .OrderByDescending(b => b.Amount)
                                .FirstOrDefault()?.Amount ?? 0,
                            NewHighestBid = outbid.NewAmount
                        });
                    break;

                case Domain.Events.BuyNowTriggeredEvent buyNow:
                    await _notificationService.NotifyAuctionEndedAsync(
                        buyNow.AuctionId,
                        new AuctionEndedNotification
                        {
                            AuctionId = buyNow.AuctionId,
                            WinnerId = buyNow.BuyerId,
                            WinningAmount = buyNow.Amount,
                            EndedAt = _dateTimeProvider.UtcNow
                        });

                    _logger.LogInformation(
                        "BuyNow triggered on auction {AuctionId} by {BuyerId} at {Amount}",
                        buyNow.AuctionId, buyNow.BuyerId, buyNow.Amount);
                    break;
            }
        }

        auction.ClearDomainEvents();
    }
}