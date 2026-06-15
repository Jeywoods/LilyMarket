using LilyMarket.Application.Interfaces;
using LilyMarket.Infrastructure.Auth;
using LilyMarket.Infrastructure.BackgroundServices;
using LilyMarket.Infrastructure.Data;
using LilyMarket.Infrastructure.Hubs;
using LilyMarket.Infrastructure.Repositories;
using LilyMarket.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LilyMarket.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuctionRepository, AuctionRepository>();
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<INotificationService, NotificationService>();

        services.AddHostedService<AuctionExpiryService>();
        services.AddHostedService<AuctionEndingSoonService>();

        services.AddSignalR();

        return services;
    }
}