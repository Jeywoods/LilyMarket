namespace LilyMarket.Application.DTO.Auctions;

public class UpdateAuctionRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? BuyNowPrice { get; set; }
}