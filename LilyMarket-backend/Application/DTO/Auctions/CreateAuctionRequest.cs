namespace LilyMarket.Application.DTO.Auctions;

public class CreateAuctionRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal MinimumIncrement { get; set; }
    public decimal? BuyNowPrice { get; set; }
    public DateTime EndTime { get; set; }
}