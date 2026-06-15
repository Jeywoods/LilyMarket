using FluentValidation;
using LilyMarket.Application.DTO.Auctions;

namespace LilyMarket.Application.Validators;

public class CreateAuctionRequestValidator : AbstractValidator<CreateAuctionRequest>
{
    public CreateAuctionRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.StartingPrice)
            .GreaterThan(0);

        RuleFor(x => x.MinimumIncrement)
            .GreaterThan(0);

        RuleFor(x => x.EndTime)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("End time must be in the future");

        RuleFor(x => x.BuyNowPrice)
            .GreaterThan(x => x.StartingPrice)
            .When(x => x.BuyNowPrice.HasValue)
            .WithMessage("BuyNow price must be greater than starting price");
    }
}