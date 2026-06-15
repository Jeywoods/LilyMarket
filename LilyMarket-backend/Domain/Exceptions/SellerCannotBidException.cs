namespace LilyMarket.Domain.Exceptions;

public class SellerCannotBidException : BidValidationException
{
    public SellerCannotBidException(Guid sellerId, Guid auctionId)
        : base(
            "SELLER_CANNOT_BID",
            $"Seller {sellerId} cannot bid on their own auction {auctionId}")
    {
    }
}