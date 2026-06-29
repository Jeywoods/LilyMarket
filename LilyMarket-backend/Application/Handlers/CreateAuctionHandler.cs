using FluentValidation;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class CreateAuctionHandler
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateAuctionRequest> _validator;
    private readonly ILogger<CreateAuctionHandler> _logger;

    public CreateAuctionHandler(
        IAuctionRepository auctionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateAuctionRequest> validator,
        ILogger<CreateAuctionHandler> logger)
    {
        _auctionRepository = auctionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AuctionDetailDto> Handle(Guid sellerId, CreateAuctionRequest request, CancellationToken ct = default)
    {
        //проверяем входные данные: название не пустое, цена > 0, время в будущем
        await _validator.ValidateAndThrowAsync(request, ct);

        //проверяем что продавец существует в базе
        var seller = await _userRepository.GetByIdAsync(sellerId, ct);
        if (seller is null)
            throw new InvalidOperationException("Seller not found");

        //создаём новый аукцион
        //StartedAt = серверное время сейчас, Status = Active
        var auction = new Auction(
            sellerId,
            request.Title,
            request.Description,
            request.Category,
            request.Condition,
            request.CoverImageUrl,
            new Money(request.StartingPrice),       //оборачиваем decimal в Money (проверка > 0 + округление)
            new Money(request.MinimumIncrement),    //шаг ставки
            request.BuyNowPrice.HasValue ? new Money(request.BuyNowPrice.Value) : null,  //цена выкупа — необязательно
            request.EndTime,                        //когда закончится
            _dateTimeProvider.UtcNow);              //серверное время начала

        //сохраняем в базу
        await _auctionRepository.AddAsync(auction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Auction {AuctionId} created by seller {SellerId}, ends at {EndTime}",
            auction.Id, sellerId, auction.EndTime);

        //возвращаем DTO — клиент увидит созданный аукцион
        return new AuctionDetailDto
        {
            Id = auction.Id,
            SellerId = auction.SellerId,
            SellerName = seller.DisplayName,          //имя продавца из User
            Title = auction.Title,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            MinimumIncrement = auction.MinimumIncrement,
            BuyNowPrice = auction.BuyNowPrice,
            CurrentHighestBid = auction.CurrentHighestBid,        //null — ставок ещё нет
            CurrentHighestBidderId = auction.CurrentHighestBidderId, //null
            StartedAt = auction.StartedAt,
            EndTime = auction.EndTime,
            Status = auction.Status.ToString()       //"Active"
        };
    }
}