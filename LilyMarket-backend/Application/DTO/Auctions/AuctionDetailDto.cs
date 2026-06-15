using LilyMarket.Application.DTO.Bids;

namespace LilyMarket.Application.DTO.Auctions;

public class AuctionDetailDto
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal MinimumIncrement { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public decimal? CurrentHighestBid { get; set; }
    public Guid? CurrentHighestBidderId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<BidSummaryDto> RecentBids { get; set; } = new();
}