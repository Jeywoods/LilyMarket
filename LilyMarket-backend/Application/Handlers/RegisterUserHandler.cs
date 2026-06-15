using FluentValidation;
using LilyMarket.Application.DTO.Auth;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<RegisterRequest> _validator;
    private readonly ILogger<RegisterUserHandler> _logger;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        IValidator<RegisterRequest> validator,
        ILogger<RegisterUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(RegisterRequest request, CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        if (_userRepository.ExistsByEmail(request.Email))
            throw new InvalidOperationException("User with this email already exists");

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(
            request.Email,
            request.DisplayName,
            passwordHash,
            _dateTimeProvider.UtcNow);

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var token = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation("User {UserId} registered with email {Email}", user.Id, user.Email);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(24),
            UserId = user.Id,
            DisplayName = user.DisplayName
        };
    }
}