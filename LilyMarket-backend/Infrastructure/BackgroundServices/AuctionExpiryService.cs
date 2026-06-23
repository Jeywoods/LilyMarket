using LilyMarket.Application.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LilyMarket.Infrastructure.BackgroundServices;

public class AuctionExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionExpiryService> _logger;

    public AuctionExpiryService(IServiceScopeFactory scopeFactory, ILogger<AuctionExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //бесконечный цикл пока приложение работает
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                //получаем EndExpiredAuctionsHandler который ищет истёкшие аукционы
                //определяет победителей и рассылает уведомления
                var handler = scope.ServiceProvider.GetRequiredService<EndExpiredAuctionsHandler>();
                await handler.Handle(stoppingToken);
            }
            catch (Exception ex)
            {
                //логируем ошибку, сервис продолжает работать
                //через 30 секунд попробует снова
                _logger.LogError(ex, "Error ending expired auctions");
            }

            //ждём 30 секунд до следующей проверки
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}