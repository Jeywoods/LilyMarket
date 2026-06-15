using LilyMarket.Application.DTO.Auth;
using LilyMarket.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Application.Handlers;

public class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LoginUserHandler> _logger;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IDateTimeProvider dateTimeProvider,
        ILogger<LoginUserHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<AuthResponse> Handle(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        var token = _jwtTokenService.GenerateToken(user);

        _logger.LogInformation("User {UserId} logged in", user.Id);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = _dateTimeProvider.UtcNow.AddHours(24),
            UserId = user.Id,
            DisplayName = user.DisplayName
        };
    }
}