using LilyMarket.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Infrastructure.BackgroundServices;

public class AuctionEndingSoonService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionEndingSoonService> _logger;

    public AuctionEndingSoonService(IServiceScopeFactory scopeFactory, ILogger<AuctionEndingSoonService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

                var now = dateTimeProvider.UtcNow;
                var endingSoon = await repo.GetExpiredActiveAuctionsAsync(now.AddMinutes(6), stoppingToken);

                foreach (var auction in endingSoon.Where(a => a.EndTime >= now.AddMinutes(4) && a.EndTime <= now.AddMinutes(6)))
                {
                    await notificationService.NotifyAuctionEndingSoonAsync(auction.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ending soon auctions");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}