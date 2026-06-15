namespace LilyMarket.Domain.Events;

public record AuctionEndedNoWinnerEvent(
    Guid AuctionId,
    Guid SellerId
    );