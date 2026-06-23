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
        //крутимся бесконечно пока приложение не остановят
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAuctionRepository>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

                var now = dateTimeProvider.UtcNow;

                //ищем активные аукционы которые закончатся в ближайшие 6 минут
                var endingSoon = await repo.GetExpiredActiveAuctionsAsync(now.AddMinutes(6), stoppingToken);

                //из них выбираем те что закончатся через 4-6 минут
                //это окно в 2 минуты чтобы точно не пропустить и не задвоить уведомление
                foreach (var auction in endingSoon.Where(a => a.EndTime >= now.AddMinutes(4) && a.EndTime <= now.AddMinutes(6)))
                {
                    //отправляем всем кто смотрит аукцион: "аукцион закончится через 5 минут"
                    await notificationService.NotifyAuctionEndingSoonAsync(auction.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ending soon auctions");
            }

            //ждём минуту перед следующей проверкой
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}