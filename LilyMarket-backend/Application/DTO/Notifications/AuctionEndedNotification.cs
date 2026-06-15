namespace LilyMarket.Application.DTO.Notifications;

public class AuctionEndedNotification
{
    public Guid AuctionId { get; set; }
    public Guid? WinnerId { get; set; }
    public decimal? WinningAmount { get; set; }
    public DateTime EndedAt { get; set; }
}