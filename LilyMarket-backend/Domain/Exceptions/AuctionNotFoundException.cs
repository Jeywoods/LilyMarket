namespace LilyMarket.Domain.Exceptions;

public class AuctionNotFoundException : Exception
{
    public string Code => "AUCTION_NOT_FOUND";

    public AuctionNotFoundException(Guid auctionId)
        : base($"Auction with ID {auctionId} was not found")
    {
    }
}