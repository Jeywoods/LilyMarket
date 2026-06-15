using FluentAssertions;
using LilyMarket.Application.DTO.Auth;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Handlers;

public class LoginUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<ILogger<LoginUserHandler>> _loggerMock = new();

    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _dateTimeProviderMock.Object,
            _loggerMock.Object);

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsToken()
    {
        var user = new User("student@sfedu.ru", "Ivan", "hashedpassword", DateTime.UtcNow);
        _userRepoMock.Setup(x => x.GetByEmailAsync("student@sfedu.ru", default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("password123", "hashedpassword")).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(user)).Returns("token123");

        var result = await _handler.Handle(new LoginRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123"
        });

        result.Token.Should().Be("token123");
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User("student@sfedu.ru", "Ivan", "hashedpassword", DateTime.UtcNow);
        _userRepoMock.Setup(x => x.GetByEmailAsync("student@sfedu.ru", default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("wrongpassword", "hashedpassword")).Returns(false);

        var act = () => _handler.Handle(new LoginRequest
        {
            Email = "student@sfedu.ru",
            Password = "wrongpassword"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        _userRepoMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), default)).ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new LoginRequest
        {
            Email = "nonexistent@sfedu.ru",
            Password = "password123"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ValidLogin_ReturnsDisplayName()
    {
        var user = new User("student@sfedu.ru", "Ivan", "hashedpassword", DateTime.UtcNow);
        _userRepoMock.Setup(x => x.GetByEmailAsync("student@sfedu.ru", default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.Verify("password123", "hashedpassword")).Returns(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(user)).Returns("token123");

        var result = await _handler.Handle(new LoginRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123"
        });

        result.DisplayName.Should().Be("Ivan");
    }
}