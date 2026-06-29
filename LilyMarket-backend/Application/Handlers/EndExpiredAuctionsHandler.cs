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
        //текущее серверное время
        var now = _dateTimeProvider.UtcNow;

        //находим все активные аукционы у которых EndTime уже прошёл
        var expiredAuctions = await _auctionRepository.GetExpiredActiveAuctionsAsync(now, ct);

        foreach (var auction in expiredAuctions)
        {
            //вызываем доменный метод End()
            //переводит статус в Ended, определяет победителя или фиксирует что ставок не было
            //генерирует доменные события: AuctionEndedEvent или AuctionEndedNoWinnerEvent
            auction.End(now);
            _auctionRepository.Update(auction);

            //обрабатываем доменные события — рассылаем уведомления
            foreach (var domainEvent in auction.DomainEvents)
            {
                switch (domainEvent)
                {
                    case Domain.Events.AuctionEndedEvent ended:
                        //были ставки, есть победитель
                        //уведомляем всех кто смотрит аукцион (группа auction-{id})
                        await _notificationService.NotifyAuctionEndedAsync(
                            auction.Id,
                            new AuctionEndedNotification
                            {
                                AuctionId = ended.AuctionId,
                                WinnerId = ended.WinnerId,          //кто победил
                                WinningAmount = ended.WinningAmount, //сумма победы
                                EndedAt = now
                            });

                        _logger.LogInformation(
                            "Auction {AuctionId} ended. Winner: {WinnerId}, Amount: {Amount}",
                            auction.Id, ended.WinnerId, ended.WinningAmount);
                        break;

                    case Domain.Events.AuctionEndedNoWinnerEvent noWinner:
                        //ставок не было, победителя нет
                        //уведомляем только продавца
                        await _notificationService.NotifySellerNoWinnerAsync(
                            noWinner.SellerId, auction.Id);

                        _logger.LogInformation(
                            "Auction {AuctionId} ended with no bids", auction.Id);
                        break;
                }
            }

            //очищаем события чтобы не разослать повторно
            auction.ClearDomainEvents();
        }

        //если были завершённые аукционы — сохраняем изменения в БД одной транзакцией
        if (expiredAuctions.Any())
            await _unitOfWork.SaveChangesAsync(ct);
    }
}