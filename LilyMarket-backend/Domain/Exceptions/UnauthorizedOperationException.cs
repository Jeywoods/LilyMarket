namespace LilyMarket.Domain.Exceptions;

public class UnauthorizedOperationException : Exception
{
    public string Code => "UNAUTHORIZED_OPERATION";

    public UnauthorizedOperationException(string message) : base(message)
    {
    }
}