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
        //сначала валидируем запрос: сумма больше нуля и т.д.
        await _validator.ValidateAndThrowAsync(request, ct);

        //сюда запишем реальную наивысшую ставку после сохранения
        decimal newHighestBid = 0;

        //оборачиваем всё в транзакцию чтобы две одновременные ставки не сломали состояние
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            //если другая транзакция уже забрала блокировку эта будет ждать
            var auction = await _auctionRepository.GetByIdWithBidsForUpdateAsync(auctionId, ct);

            //аукцион не найден кидаем 404
            if (auction is null)
                throw new AuctionNotFoundException(auctionId);

            //создаём ставку, серверное время из IDateTimeProvider
            var bid = new Bid(
                auctionId,
                bidderId,
                new Domain.ValueObjects.Money(request.Amount),
                _dateTimeProvider.UtcNow);

            //проверка статуса, времени, продавца, минимальной суммы, BuyNow
            auction.PlaceBid(bid, _dateTimeProvider.UtcNow);

            //сохраняем ставку и обновлённый аукцион
            await _bidRepository.AddAsync(bid, ct);
            _auctionRepository.Update(auction);
            await _unitOfWork.SaveChangesAsync(ct);

            //запоминаем реальную сумму, она могла измениться внутри PlaceBid
            newHighestBid = auction.CurrentHighestBid!.Value;

            //рассылаем уведомления через SignalR
            await DispatchEvents(auction);

            _logger.LogInformation(
                "Bid of {Amount} placed on auction {AuctionId} by {BidderId}, new highest: {NewHighest}",
                request.Amount, auctionId, bidderId, auction.CurrentHighestBid);
        }, ct);

        //транзакция прошла успешно возвращаем результат
        return new BidResultDto
        {
            Success = true,
            NewHighestBid = newHighestBid,
            Message = "Bid placed successfully"
        };
    }

    //обрабатываем доменные события и превращаем их в уведомления через SignalR
    private async Task DispatchEvents(Auction auction)
    {
        foreach (var domainEvent in auction.DomainEvents)
        {
            switch (domainEvent)
            {
                case Domain.Events.BidPlacedEvent bidPlaced:
                    //новая ставка: уведомляем всех кто смотрит этот аукцион
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
                    //перебили чью-то ставку: находим его предыдущую сумму
                    var previousBidAmount = auction.Bids
                        .Where(b => b.BidderId == outbid.PreviousHighestBidderId)
                        .OrderByDescending(b => b.Amount)
                        .Skip(1)  //пропускаем текущую наивысшую, берём предыдущую этого же участника
                        .FirstOrDefault()?.Amount ?? 0;

                    //уведомляем только перебитого участника
                    await _notificationService.NotifyOutbidAsync(
                        outbid.PreviousHighestBidderId,
                        new OutbidNotification
                        {
                            AuctionId = outbid.AuctionId,
                            YourPreviousBid = previousBidAmount,
                            NewHighestBid = outbid.NewAmount
                        });
                    break;

                case Domain.Events.BuyNowTriggeredEvent buyNow:
                    //ставка достигла цены выкупа — аукцион закрыт
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

        //очищаем события, чтобы не разослать повторно
        auction.ClearDomainEvents();
    }
}