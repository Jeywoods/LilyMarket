using LilyMarket.Domain.Entities;

namespace LilyMarket.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}