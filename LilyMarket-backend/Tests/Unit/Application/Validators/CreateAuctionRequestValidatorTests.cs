using FluentAssertions;
using FluentValidation.TestHelper;
using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Validators;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Validators;

public class CreateAuctionRequestValidatorTests
{
    private readonly CreateAuctionRequestValidator _validator = new();

    private CreateAuctionRequest ValidRequest()
    {
        return new CreateAuctionRequest
        {
            Title = "Test Item",
            Description = "Test Description",
            StartingPrice = 100,
            MinimumIncrement = 10,
            EndTime = DateTime.UtcNow.AddDays(7)
        };
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyTitle_Fails()
    {
        var request = ValidRequest();
        request.Title = "";
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void EmptyDescription_Fails()
    {
        var request = ValidRequest();
        request.Description = "";
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void TitleTooLong_Fails()
    {
        var request = ValidRequest();
        request.Title = new string('A', 201);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void DescriptionTooLong_Fails()
    {
        var request = ValidRequest();
        request.Description = new string('A', 2001);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ZeroStartingPrice_Fails()
    {
        var request = ValidRequest();
        request.StartingPrice = 0;
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartingPrice);
    }

    [Fact]
    public void NegativeStartingPrice_Fails()
    {
        var request = ValidRequest();
        request.StartingPrice = -100;
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.StartingPrice);
    }

    [Fact]
    public void ZeroMinimumIncrement_Fails()
    {
        var request = ValidRequest();
        request.MinimumIncrement = 0;
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.MinimumIncrement);
    }

    [Fact]
    public void EndTimeInPast_Fails()
    {
        var request = ValidRequest();
        request.EndTime = DateTime.UtcNow.AddHours(-1);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.EndTime);
    }

    [Fact]
    public void BuyNowLessThanStartingPrice_Fails()
    {
        var request = ValidRequest();
        request.StartingPrice = 100;
        request.BuyNowPrice = 50;
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.BuyNowPrice);
    }

    [Fact]
    public void BuyNowEqualToStartingPrice_Fails()
    {
        var request = ValidRequest();
        request.StartingPrice = 100;
        request.BuyNowPrice = 100;
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.BuyNowPrice);
    }

    [Fact]
    public void BuyNowGreaterThanStartingPrice_Passes()
    {
        var request = ValidRequest();
        request.StartingPrice = 100;
        request.BuyNowPrice = 200;
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.BuyNowPrice);
    }

    [Fact]
    public void NoBuyNow_Passes()
    {
        var request = ValidRequest();
        request.BuyNowPrice = null;
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.BuyNowPrice);
    }
}