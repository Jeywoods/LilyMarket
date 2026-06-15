namespace LilyMarket.Domain.Exceptions;

public class BidTooLowException : BidValidationException
{
    public decimal RequiredMinimum { get; }

    public BidTooLowException(decimal requiredMinimum)
        : base(
            "BID_TOO_LOW",
            $"Bid must be at least {requiredMinimum:F2}. " +
            $"The minimum bid is current highest plus minimum increment.")
    {
        RequiredMinimum = requiredMinimum;
    }
}