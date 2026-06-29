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
        //получаем страницу аукционов из базы сразу со ставками и общим количеством
        var pagedAuctions = await _auctionRepository.GetPagedAsync(page, pageSize, ct);

        //преобразуем каждый аукцион в короткий DTO для списка
        var items = pagedAuctions.Items.Select(a => new AuctionSummaryDto
        {
            Id = a.Id,
            Title = a.Title,
            Category = a.Category,               //категория товара
            Condition = a.Condition,             //состояние
            CoverImageUrl = a.CoverImageUrl,     //фото
            CurrentHighestBid = a.CurrentHighestBid ?? a.StartingPrice,  //текущая цена или стартовая если ставок нет
            StartingPrice = a.StartingPrice,
            BuyNowPrice = a.BuyNowPrice,
            EndTime = a.EndTime,
            Status = a.Status.ToString(),
            BidCount = a.Bids.Count             //сколько ставок уже сделано
        }).ToList();

        return new PagedResult<AuctionSummaryDto>(items, pagedAuctions.TotalCount, page, pageSize);
    }
}