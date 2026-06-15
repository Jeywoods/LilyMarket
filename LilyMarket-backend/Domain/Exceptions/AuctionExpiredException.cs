namespace LilyMarket.Domain.Exceptions;

public class AuctionExpiredException : BidValidationException
{
    public AuctionExpiredException(Guid auctionId, DateTime endTime)
        : base(
            "AUCTION_EXPIRED",
            $"Auction {auctionId} has already ended at {endTime:O}")
    {
    }
}