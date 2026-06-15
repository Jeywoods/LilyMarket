using LilyMarket.Domain.ValueObjects;

namespace LilyMarket.Domain.Entities;

public class Bid
{
    public Guid Id { get; private set; }
    public Guid AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PlacedAt { get; private set; }

    private Bid() { } 

    public Bid(Guid auctionId, Guid bidderId, Money amount, DateTime placedAt)
    {
        Id = Guid.NewGuid();
        AuctionId = auctionId;
        BidderId = bidderId;
        Amount = amount.Amount;
        PlacedAt = placedAt;
    }
}