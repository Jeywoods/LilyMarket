namespace LilyMarket.Application.DTO.Notifications;

public class BidPlacedNotification
{
    public Guid AuctionId { get; set; }
    public decimal NewHighestBid { get; set; }
    public int BidCount { get; set; }
    public Guid BidderId { get; set; }
}