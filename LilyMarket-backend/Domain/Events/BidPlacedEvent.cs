namespace LilyMarket.Domain.Events;

public record BidPlacedEvent(
    Guid AuctionId,
    Guid BidderId,
    decimal NewHighestBid
);