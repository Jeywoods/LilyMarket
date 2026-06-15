namespace LilyMarket.Application.DTO.Bids;

public class BidResultDto
{
    public bool Success { get; set; }
    public decimal NewHighestBid { get; set; }
    public string Message { get; set; } = string.Empty;
}