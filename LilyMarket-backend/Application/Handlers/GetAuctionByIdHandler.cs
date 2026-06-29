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
        //достаём аукцион из базы вместе со всеми ставками
        var auction = await _auctionRepository.GetByIdWithBidsAsync(id, ct);

        //не найден — 404
        if (auction is null)
            throw new AuctionNotFoundException(id);

        //находим продавца чтобы показать его имя
        var seller = await _userRepository.GetByIdAsync(auction.SellerId, ct);

        //собираем 5 последних ставок для истории
        var recentBids = new List<BidSummaryDto>();
        var orderedBids = auction.Bids
            .OrderByDescending(b => b.PlacedAt)  //сортируем от новых к старым
            .Take(5);                            //берём только 5 последних

        //для каждой ставки находим имя участника
        foreach (var bid in orderedBids)
        {
            var bidder = await _userRepository.GetByIdAsync(bid.BidderId, ct);
            recentBids.Add(new BidSummaryDto
            {
                BidderId = bid.BidderId,
                BidderName = bidder?.DisplayName ?? "Unknown",  //имя или "Unknown" если пользователь удалён
                Amount = bid.Amount,           //сумма ставки
                PlacedAt = bid.PlacedAt        //когда сделана
            });
        }

        //собираем полный DTO для ответа клиенту
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
            CurrentHighestBid = auction.CurrentHighestBid,           //текущая цена (null если нет ставок)
            CurrentHighestBidderId = auction.CurrentHighestBidderId, //id лидера (null если нет ставок)
            StartedAt = auction.StartedAt,
            EndTime = auction.EndTime,
            Status = auction.Status.ToString(),  //Active, Ended, Sold, Canceled
            RecentBids = recentBids              //последние 5 ставок с именами участников
        };
    }
}