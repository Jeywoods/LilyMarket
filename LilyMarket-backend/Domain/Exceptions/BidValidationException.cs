namespace LilyMarket.Domain.Exceptions;

public class BidValidationException : Exception
{
    public string Code { get; }

    public BidValidationException(string code, string message) : base(message)
    {
        Code = code;
    }

    public BidValidationException(string code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }
}