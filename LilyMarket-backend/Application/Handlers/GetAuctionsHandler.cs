using LilyMarket.Application.Common;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Interfaces;

namespace LilyMarket.Application.Handlers;

public class GetAuctionsHandler
{
    private readonly IAuctionRepository _auctionRepository;

    public GetAuctionsHandler(IAuctionRepository auctionRepository)
    {
        _auctionRepository = auctionRepository;
    }

    public async Task<PagedResult<AuctionSummaryDto>> Handle(int page, int pageSize, CancellationToken ct = default)
    {
        var pagedAuctions = await _auctionRepository.GetPagedAsync(page, pageSize, ct);

        var items = pagedAuctions.Items.Select(a => new AuctionSummaryDto
        {
            Id = a.Id,
            Title = a.Title,
            Category = a.Category,
            Condition = a.Condition,
            CoverImageUrl = a.CoverImageUrl,
            CurrentHighestBid = a.CurrentHighestBid ?? a.StartingPrice,
            StartingPrice = a.StartingPrice,
            BuyNowPrice = a.BuyNowPrice,
            EndTime = a.EndTime,
            Status = a.Status.ToString(),
            BidCount = a.Bids.Count
        }).ToList();

        return new PagedResult<AuctionSummaryDto>(items, pagedAuctions.TotalCount, page, pageSize);
    }
}