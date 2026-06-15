using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Exceptions;

namespace LilyMarket.Application.Handlers;

public class GetAuctionByIdHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUserRepository _userRepository;

    public GetAuctionByIdHandler(
        IAuctionRepository auctionRepository,
        IUserRepository userRepository)
    {
        _auctionRepository = auctionRepository;
        _userRepository = userRepository;
    }

    public async Task<AuctionDetailDto> Handle(Guid id, CancellationToken ct = default)
    {
        var auction = await _auctionRepository.GetByIdWithBidsAsync(id, ct);

        if (auction is null)
            throw new AuctionNotFoundException(id);

        var seller = await _userRepository.GetByIdAsync(auction.SellerId, ct);

        var recentBids = auction.Bids
            .OrderByDescending(b => b.PlacedAt)
            .Take(5)
            .Select(async b =>
            {
                var bidder = await _userRepository.GetByIdAsync(b.BidderId, ct);
                return new BidSummaryDto
                {
                    BidderId = b.BidderId,
                    BidderName = bidder?.DisplayName ?? "Unknown",
                    Amount = b.Amount,
                    PlacedAt = b.PlacedAt
                };
            })
            .Select(t => t.Result)
            .ToList();

        return new AuctionDetailDto
        {
            Id = auction.Id,
            SellerId = auction.SellerId,
            SellerName = seller?.DisplayName ?? "Unknown",
            Title = auction.Title,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            MinimumIncrement = auction.MinimumIncrement,
            BuyNowPrice = auction.BuyNowPrice,
            CurrentHighestBid = auction.CurrentHighestBid,
            CurrentHighestBidderId = auction.CurrentHighestBidderId,
            StartedAt = auction.StartedAt,
            EndTime = auction.EndTime,
            Status = auction.Status.ToString(),
            RecentBids = recentBids
        };
    }
}