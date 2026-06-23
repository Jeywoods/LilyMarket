using LilyMarket.Domain.Enums;
using LilyMarket.Domain.Events;
using LilyMarket.Domain.Exceptions;
using LilyMarket.Domain.ValueObjects;

namespace LilyMarket.Domain.Entities;

public class Auction
{
    //список всех ставок на этот аукцион, менять можно только через PlaceBid
    private readonly List<Bid> _bids = new();
    //сюда складываем доменные события, потом хендлер их разошлёт через SignalR
    private readonly List<object> _domainEvents = new();

    public Guid Id { get; private set; }
    public Guid SellerId { get; private set; }           //id продавца, создавшего аукцион
    public string Title { get; private set; }             //название лота
    public string Description { get; private set; }       //описание товара
    public string Category { get; private set; }          //категория: Tech, Books, Furniture
    public string Condition { get; private set; }         //состояние: New, Like New, Good
    public string CoverImageUrl { get; private set; }     //ссылка на фото товара
    public decimal StartingPrice { get; private set; }    //начальная цена, от неё считается первая ставка
    public decimal MinimumIncrement { get; private set; } //минимальный шаг, на который нужно перебить ставку
    public decimal? BuyNowPrice { get; private set; }     //цена мгновенного выкупа, null если не задана
    public decimal? CurrentHighestBid { get; private set; }      //текущая наивысшая ставка, null если ставок нет
    public Guid? CurrentHighestBidderId { get; private set; }    //id текущего лидера, null если ставок нет
    public DateTime StartedAt { get; private set; }       //когда создали аукцион
    public DateTime EndTime { get; private set; }         //когда аукцион автоматически завершится
    public AuctionStatus Status { get; private set; }     //Active, Ended, Sold или Canceled

    //только для чтения снаружи, менять можно только через методы класса
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    //ef core использует этот конструктор когда достаёт аукцион из базы
    private Auction()
    {
        Title = string.Empty;
        Description = string.Empty;
        Category = string.Empty;
        Condition = string.Empty;
        CoverImageUrl = string.Empty;
    }

    //создание нового аукциона, проверяет что все данные корректны
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
        //название не может быть пустым или из пробелов
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        //описание обязательно
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        //время окончания должно быть в будущем
        if (endTime <= startedAt)
            throw new ArgumentException("End time must be in the future", nameof(endTime));
        //цена выкупа должна быть выше стартовой цены
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
        Status = AuctionStatus.Active;  //сразу после создания аукцион открыт для ставок
    }

    //методы для UpdateAuctionHandler, позволяют продавцу менять аукцион до первой ставки
    public void UpdateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty", nameof(title));
        Title = title;
    }

    public void UpdateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));
        Description = description;
    }

    public void UpdateBuyNowPrice(decimal? buyNowPrice)
    {
        //нельзя установить цену выкупа ниже или равной стартовой
        if (buyNowPrice.HasValue && buyNowPrice.Value <= StartingPrice)
            throw new ArgumentException(
                "BuyNow price must be greater than starting price", nameof(buyNowPrice));
        BuyNowPrice = buyNowPrice;
    }

    //главный метод — размещение ставки. здесь все правила аукциона
    public void PlaceBid(Bid bid, DateTime serverTime)
    {
        //ставки только на активный аукцион
        if (Status != AuctionStatus.Active)
            throw new AuctionExpiredException(Id, EndTime);

        //проверяем серверное время, а не клиентское
        if (serverTime >= EndTime)
            throw new AuctionExpiredException(Id, EndTime);

        //продавец не может ставить на свой же аукцион
        if (bid.BidderId == SellerId)
            throw new SellerCannotBidException(SellerId, Id);

        //вычисляем минимальную допустимую сумму
        //если ставок ещё нет — отталкиваемся от StartingPrice
        var currentHighest = CurrentHighestBid ?? StartingPrice;
        var requiredMinimum = currentHighest + MinimumIncrement;

        if (bid.Amount < requiredMinimum)
            throw new BidTooLowException(requiredMinimum);

        //запоминаем кто был лидером до этой ставки
        var previousHighestBidderId = CurrentHighestBidderId;

        //обновляем состояние аукциона
        CurrentHighestBid = bid.Amount;
        CurrentHighestBidderId = bid.BidderId;
        _bids.Add(bid);

        //новая ставка, увидят все кто смотрит этот аукцион
        _domainEvents.Add(new BidPlacedEvent(Id, bid.BidderId, bid.Amount));

        //если перебили другого участника — ему придёт отдельное уведомление
        if (previousHighestBidderId.HasValue && previousHighestBidderId != bid.BidderId)
        {
            _domainEvents.Add(new BidOutbidEvent(
                Id, previousHighestBidderId.Value, bid.Amount));
        }

        //если ставка достигла цены выкупа — закрываем аукцион мгновенно
        if (BuyNowPrice.HasValue && bid.Amount >= BuyNowPrice.Value)
        {
            Status = AuctionStatus.Sold;
            _domainEvents.Add(new BuyNowTriggeredEvent(Id, bid.BidderId, bid.Amount));
        }
    }

    //отмена аукциона продавцом
    public void Cancel(Guid requesterId)
    {
        //только продавец может отменить свой аукцион
        if (requesterId != SellerId)
            throw new UnauthorizedOperationException(
                "Only the seller can cancel this auction");

        //нельзя отменить если уже есть хотя бы одна ставка
        if (_bids.Count > 0)
            throw new UnauthorizedOperationException(
                "Cannot cancel auction with existing bids");

        Status = AuctionStatus.Canceled;
    }

    //завершение аукциона по истечению времени, вызывает фоновый сервис
    public void End(DateTime serverTime)
    {
        //если аукцион уже не активен — ничего не делаем
        if (Status != AuctionStatus.Active)
            return;

        Status = AuctionStatus.Ended;

        //если были ставки — определяем победителя
        if (CurrentHighestBidderId.HasValue && CurrentHighestBid.HasValue)
        {
            _domainEvents.Add(new AuctionEndedEvent(
                Id, CurrentHighestBidderId.Value, CurrentHighestBid.Value));
        }
        //если ставок не было — уведомим продавца что никто не участвовал
        else
        {
            _domainEvents.Add(new AuctionEndedNoWinnerEvent(Id, SellerId));
        }
    }

    //очищаем события после того как хендлер их обработал и разослал
    public void ClearDomainEvents() => _domainEvents.Clear();
}