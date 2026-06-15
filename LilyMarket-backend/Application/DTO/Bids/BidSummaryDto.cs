namespace LilyMarket.Application.DTO.Bids;

public class BidSummaryDto
{
    public Guid BidderId { get; set; }
    public string BidderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; }
}