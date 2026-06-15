using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LilyMarket.Application.DTO.Auth;
using LilyMarket.Application.Handlers;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LilyMarket.Tests.Unit.Application.Handlers;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IValidator<RegisterRequest>> _validatorMock = new();
    private readonly Mock<ILogger<RegisterUserHandler>> _loggerMock = new();

    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(
            _userRepoMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _dateTimeProviderMock.Object,
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
        _validatorMock.Setup(x => x.ValidateAsync(It.IsAny<RegisterRequest>(), default))
            .ReturnsAsync(new ValidationResult());
        _passwordHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashedpassword");
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("token123");
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesUser()
    {
        _userRepoMock.Setup(x => x.ExistsByEmail(It.IsAny<string>())).Returns(false);

        var request = new RegisterRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123",
            DisplayName = "Ivan"
        };

        var result = await _handler.Handle(request);

        result.Token.Should().Be("token123");
        result.DisplayName.Should().Be("Ivan");
        _userRepoMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsInvalidOperationException()
    {
        _userRepoMock.Setup(x => x.ExistsByEmail(It.IsAny<string>())).Returns(true);

        var request = new RegisterRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123",
            DisplayName = "Ivan"
        };

        var act = () => _handler.Handle(request);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsUserId()
    {
        _userRepoMock.Setup(x => x.ExistsByEmail(It.IsAny<string>())).Returns(false);

        var result = await _handler.Handle(new RegisterRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123",
            DisplayName = "Ivan"
        });

        result.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsExpiresAt()
    {
        _userRepoMock.Setup(x => x.ExistsByEmail(It.IsAny<string>())).Returns(false);

        var result = await _handler.Handle(new RegisterRequest
        {
            Email = "student@sfedu.ru",
            Password = "password123",
            DisplayName = "Ivan"
        });

        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }
}