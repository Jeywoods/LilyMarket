using LilyMarket.Domain.Enums;
using LilyMarket.Domain.Events;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;

namespace LilyMarket.Domain.Entities;

public class Auction
{
    private readonly List<Bid> _bids = new();
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public string Condition { get; private set; }
    public string CoverImageUrl { get; private set; }
    public decimal StartingPrice { get; private set; }
    public decimal MinimumIncrement { get; private set; }
    public decimal? BuyNowPrice { get; private set; }
    public decimal? CurrentHighestBid { get; private set; }
    public Guid? CurrentHighestBidderId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime EndTime { get; private set; }
    public AuctionStatus Status { get; private set; }
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    private Auction()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        Condition = string.Empty;
        CoverImageUrl = string.Empty;
    }

    public Auction(
        Guid sellerId,
        string title,
        string description,
        string category,
        string condition,
        string coverImageUrl,
        Money startingPrice,
        Money minimumIncrement,
        Money? buyNowPrice,
        DateTime endTime,
        DateTime startedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        if (endTime <= startedAt)
            throw new ArgumentException("End time must be in the future", nameof(endTime));
        if (buyNowPrice is not null && buyNowPrice <= startingPrice)
            throw new ArgumentException(
                "BuyNow price must be greater than starting price", nameof(buyNowPrice));

        Id = Guid.NewGuid();
        SellerId = sellerId;
        Title = title;
        Description = description;
        Category = category;
        Condition = condition;
        CoverImageUrl = coverImageUrl;
        StartingPrice = startingPrice.Amount;
        MinimumIncrement = minimumIncrement.Amount;
        BuyNowPrice = buyNowPrice?.Amount;
        EndTime = endTime;
        StartedAt = startedAt;
        Status = AuctionStatus.Active;
    }

    public void PlaceBid(Bid bid, DateTime serverTime)
    {
        if (Status != AuctionStatus.Active)
            throw new AuctionExpiredException(Id, EndTime);

        if (serverTime >= EndTime)
            throw new AuctionExpiredException(Id, EndTime);

        if (bid.BidderId == SellerId)
            throw new SellerCannotBidException(SellerId, Id);

        var currentHighest = CurrentHighestBid ?? StartingPrice;
        var requiredMinimum = currentHighest + MinimumIncrement;

        if (bid.Amount < requiredMinimum)
            throw new BidTooLowException(requiredMinimum);

        var previousHighestBidderId = CurrentHighestBidderId;

        CurrentHighestBid = bid.Amount;
        CurrentHighestBidderId = bid.BidderId;
        _bids.Add(bid);

        _domainEvents.Add(new BidPlacedEvent(Id, bid.BidderId, bid.Amount, bid.Amount));

        if (previousHighestBidderId.HasValue && previousHighestBidderId != bid.BidderId)
        {
            _domainEvents.Add(new BidOutbidEvent(
                Id, previousHighestBidderId.Value, bid.Amount));
        }

        if (BuyNowPrice.HasValue && bid.Amount >= BuyNowPrice.Value)
        {
            Status = AuctionStatus.Sold;
            _domainEvents.Add(new BuyNowTriggeredEvent(Id, bid.BidderId, bid.Amount));
        }
    }

    public void Cancel(Guid requesterId)
    {
        if (requesterId != SellerId)
            throw new UnauthorizedOperationException(
                "Only the seller can cancel this auction");

        if (_bids.Count > 0)
            throw new UnauthorizedOperationException(
                "Cannot cancel auction with existing bids");

        Status = AuctionStatus.Canceled;
    }

    public void End(DateTime serverTime)
    {
        if (Status != AuctionStatus.Active)
            return;

        Status = AuctionStatus.Ended;

        if (CurrentHighestBidderId.HasValue)
        {
            _domainEvents.Add(new AuctionEndedEvent(
                Id, CurrentHighestBidderId.Value, CurrentHighestBid));
        }
        else
        {
            _domainEvents.Add(new AuctionEndedNoWinnerEvent(Id, SellerId));
        }
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}