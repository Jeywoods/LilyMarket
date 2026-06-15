using LilyMarket.Application.Interfaces;

namespace LilyMarket.Infrastructure.Services;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}