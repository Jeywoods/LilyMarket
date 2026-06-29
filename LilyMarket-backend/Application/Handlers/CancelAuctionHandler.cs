using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class CancelAuctionHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelAuctionHandler> _logger;

    public CancelAuctionHandler(
        IAuctionRepository auctionRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelAuctionHandler> logger)
    {
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(Guid auctionId, Guid userId, CancellationToken ct = default)
    {
        //достаём аукцион вместе со ставками, нужно проверить есть ли ставки
        var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, ct);

        //аукцион не найден — 404
        if (auction is null)
            throw new AuctionNotFoundException(auctionId);

        //проверка на то что нет ставок у лота
        auction.Cancel(userId);

        //помечаем аукцион как изменённый и сохраняем в БД
        _auctionRepository.Update(auction);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Auction {AuctionId} canceled by user {UserId}", auctionId, userId);
    }
}