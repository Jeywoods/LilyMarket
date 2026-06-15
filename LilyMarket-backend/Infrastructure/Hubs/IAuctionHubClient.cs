using LilyMarket.Application.DTO.Notifications;

namespace LilyMarket.Infrastructure.Hubs;

public interface IAuctionHubClient
{
    Task BidPlaced(BidPlacedNotification notification);
    Task Outbid(OutbidNotification notification);
    Task AuctionEnded(AuctionEndedNotification notification);
    Task AuctionEndingSoon(Guid auctionId, int minutesRemaining);
}