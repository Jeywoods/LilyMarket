using FluentValidation;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class UpdateAuctionHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAuctionHandler> _logger;

    public UpdateAuctionHandler(
        IAuctionRepository auctionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAuctionHandler> logger)
    {
        _auctionRepository = auctionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AuctionDetailDto> Handle(Guid auctionId, Guid userId, UpdateAuctionRequest request, CancellationToken ct = default)
    {
        var auction = await _auctionRepository.GetByIdAsync(auctionId, ct);

        if (auction is null)
            throw new AuctionNotFoundException(auctionId);

        if (auction.SellerId != userId)
            throw new UnauthorizedOperationException("Only the seller can update this auction");

        if (auction.Bids.Count > 0)
            throw new UnauthorizedOperationException("Cannot update auction with existing bids");

        // Явное обновление свойств вместо рефлексии
        if (request.Title is not null)
            auction.UpdateTitle(request.Title);

        if (request.Description is not null)
            auction.UpdateDescription(request.Description);

        if (request.BuyNowPrice.HasValue)
            auction.UpdateBuyNowPrice(request.BuyNowPrice.Value);

        _auctionRepository.Update(auction);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Auction {AuctionId} updated by user {UserId}", auctionId, userId);

        return new AuctionDetailDto
        {
            Id = auction.Id,
            SellerId = auction.SellerId,
            Title = auction.Title,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            MinimumIncrement = auction.MinimumIncrement,
            BuyNowPrice = auction.BuyNowPrice,
            CurrentHighestBid = auction.CurrentHighestBid,
            CurrentHighestBidderId = auction.CurrentHighestBidderId,
            StartedAt = auction.StartedAt,
            EndTime = auction.EndTime,
            Status = auction.Status.ToString()
        };
    }
}