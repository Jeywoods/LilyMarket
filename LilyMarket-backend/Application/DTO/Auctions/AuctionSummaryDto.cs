namespace LilyMarket.Application.DTO.Auctions;

public class AuctionSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public decimal CurrentHighestBid { get; set; }
    public decimal StartingPrice { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BidCount { get; set; }
}