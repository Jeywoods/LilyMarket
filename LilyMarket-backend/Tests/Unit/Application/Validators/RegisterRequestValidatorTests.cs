using FluentAssertions;
using FluentValidation.TestHelper;
using LilyMarket.Application.DTO.Auth;
using LilyMarket.Application.Validators;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new RegisterRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123",
            DisplayName = "Ivan"
        };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var request = new RegisterRequest { Email = "", Password = "password123", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailWithoutAtSign_Fails()
    {
        var request = new RegisterRequest { Email = "studentsfedu.ru", Password = "password123", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void EmailWrongDomain_Fails()
    {
        var request = new RegisterRequest { Email = "student@gmail.com", Password = "password123", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email must end with @sfedu.ru");
    }

    [Fact]
    public void EmailNotEndingWithSfedu_Fails()
    {
        var request = new RegisterRequest { Email = "student@sfedu.russia", Password = "password123", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShortPassword_Fails()
    {
        var request = new RegisterRequest { Email = "student@sfedu.ru", Password = "1234567", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void EmptyPassword_Fails()
    {
        var request = new RegisterRequest { Email = "student@sfedu.ru", Password = "", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void EmptyDisplayName_Fails()
    {
        var request = new RegisterRequest { Email = "student@sfedu.ru", Password = "password123", DisplayName = "" };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void VeryLongDisplayName_Fails()
    {
        var request = new RegisterRequest { Email = "student@sfedu.ru", Password = "password123", DisplayName = new string('A', 101) };
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ExactlyMinPassword_Passes()
    {
        var request = new RegisterRequest { Email = "student@sfedu.ru", Password = "12345678", DisplayName = "Ivan" };
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}