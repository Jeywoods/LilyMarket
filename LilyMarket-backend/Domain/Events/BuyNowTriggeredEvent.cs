namespace LilyMarket.Domain.Events;

public record BuyNowTriggeredEvent(
    Guid AuctionId,
    Guid BuyerId,
    decimal Amount
    );