using LilyMarket.Application.DTO.Notifications;
using LilyMarket.Application.Interfaces;
using LilyMarket.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LilyMarket.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<AuctionHub, IAuctionHubClient> _hubContext;

    public NotificationService(IHubContext<AuctionHub, IAuctionHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBidPlacedAsync(Guid auctionId, BidPlacedNotification notification)
    {
        await _hubContext.Clients.Group($"auction-{auctionId}").BidPlaced(notification);
    }

    public async Task NotifyOutbidAsync(Guid previousBidderId, OutbidNotification notification)
    {
        await _hubContext.Clients.Group($"user-{previousBidderId}").Outbid(notification);
    }

    public async Task NotifyAuctionEndedAsync(Guid auctionId, AuctionEndedNotification notification)
    {
        await _hubContext.Clients.Group($"auction-{auctionId}").AuctionEnded(notification);
    }

    public async Task NotifyAuctionEndingSoonAsync(Guid auctionId)
    {
        await _hubContext.Clients.Group($"auction-{auctionId}").AuctionEndingSoon(auctionId, 5);
    }

    public async Task NotifySellerNoWinnerAsync(Guid sellerId, Guid auctionId)
    {
        await _hubContext.Clients.Group($"user-{sellerId}")
            .AuctionEnded(new AuctionEndedNotification
            {
                AuctionId = auctionId,
                WinnerId = null,
                WinningAmount = null,
                EndedAt = DateTime.UtcNow
            });
    }
}