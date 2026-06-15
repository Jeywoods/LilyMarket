namespace LilyMarket.Application.DTO.Notifications;

public class OutbidNotification
{
    public Guid AuctionId { get; set; }
    public decimal YourPreviousBid { get; set; }
    public decimal NewHighestBid { get; set; }
}