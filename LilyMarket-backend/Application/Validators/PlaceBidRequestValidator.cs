using FluentValidation;
using LilyMarket.Application.DTO.Bids;

namespace LilyMarket.Application.Validators;

public class PlaceBidRequestValidator : AbstractValidator<PlaceBidRequest>
{
    public PlaceBidRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0);
    }
}