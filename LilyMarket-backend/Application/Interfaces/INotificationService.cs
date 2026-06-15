using LilyMarket.Application.DTO.Notifications;

namespace LilyMarket.Application.Interfaces;

public interface INotificationService
{
    Task NotifyBidPlacedAsync(Guid auctionId, BidPlacedNotification notification);
    Task NotifyOutbidAsync(Guid previousBidderId, OutbidNotification notification);
    Task NotifyAuctionEndedAsync(Guid auctionId, AuctionEndedNotification notification);
    Task NotifyAuctionEndingSoonAsync(Guid auctionId);
    Task NotifySellerNoWinnerAsync(Guid sellerId, Guid auctionId);
}