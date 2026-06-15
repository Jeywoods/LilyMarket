namespace LilyMarket.Domain.Events;

public record BidOutbidEvent(
    Guid AuctionId,
    Guid PreviousHighestBidderId,
    decimal NewAmount
    );