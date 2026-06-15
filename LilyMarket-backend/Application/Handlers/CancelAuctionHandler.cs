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
        var auction = await _auctionRepository.GetByIdWithBidsAsync(auctionId, ct);

        if (auction is null)
            throw new AuctionNotFoundException(auctionId);

        auction.Cancel(userId);

        _auctionRepository.Update(auction);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Auction {AuctionId} canceled by user {UserId}", auctionId, userId);
    }
}