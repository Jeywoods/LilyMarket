using FluentAssertions;
using FluentValidation.TestHelper;
using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.Validators;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Validators;

public class PlaceBidRequestValidatorTests
{
    private readonly PlaceBidRequestValidator _validator = new();

    [Fact]
    public void PositiveAmount_Passes()
    {
        var request = new PlaceBidRequest { Amount = 100 };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ZeroAmount_Fails()
    {
        var request = new PlaceBidRequest { Amount = 0 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void NegativeAmount_Fails()
    {
        var request = new PlaceBidRequest { Amount = -100 };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void LargeAmount_Passes()
    {
        var request = new PlaceBidRequest { Amount = 999999.99m };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SmallAmount_Passes()
    {
        var request = new PlaceBidRequest { Amount = 0.01m };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}