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
        await _validator.ValidateAndThrowAsync(request, ct);

        var seller = await _userRepository.GetByIdAsync(sellerId, ct);
        if (seller is null)
            throw new InvalidOperationException("Seller not found");

        var auction = new Auction(
            sellerId,
            request.Title,
            request.Description,
            request.Category,
            request.Condition,
            request.CoverImageUrl,
            new Money(request.StartingPrice),
            new Money(request.MinimumIncrement),
            request.BuyNowPrice.HasValue ? new Money(request.BuyNowPrice.Value) : null,
            request.EndTime,
            _dateTimeProvider.UtcNow);

        await _auctionRepository.AddAsync(auction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Auction {AuctionId} created by seller {SellerId}, ends at {EndTime}",
            auction.Id, sellerId, auction.EndTime);

        return new AuctionDetailDto
        {
            Id = auction.Id,
            SellerId = auction.SellerId,
            SellerName = seller.DisplayName,
            Title = auction.Title,
            Description = auction.Description,
            StartingPrice = auction.StartingPrice,
            MinimumIncrement = auction.MinimumIncrement,
            BuyNowPrice = auction.BuyNowPrice,
            CurrentHighestBid = auction.CurrentHighestBid,
            CurrentHighestBidderId = auction.CurrentHighestBidderId,
            StartedAt = auction.StartedAt,
            EndTime = auction.EndTime,
            Status = auction.Status.ToString()
        };
    }
}