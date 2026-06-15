namespace LilyMarket.Domain.Events;

public record AuctionEndedEvent(
    Guid AuctionId,
    Guid? WinnerId,
    decimal? WinningAmount
    );