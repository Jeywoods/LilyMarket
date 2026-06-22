using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LilyMarket.Infrastructure.Hubs;

[Authorize]  
public class AuctionHub : Hub<IAuctionHubClient>
{
    //клиент вызывает этот метод чтобы подписаться на обновления аукциона
    //после этого он будет получать BidPlaced, AuctionEnded и т.д.
    public async Task JoinAuction(Guid auctionId)
    {
        //добавляем соединение в группу auction-{id}
        //сервер шлёт уведомления всем кто в группе
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
    }

    //отписаться от обновлений аукциона
    public async Task LeaveAuction(Guid auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
    }

    //когда клиент подключается к хабу — автоматом добавляем его в персональную группу
    //это нужно чтобы отправлять личные уведомления: перебитие ставки, победа
    public override async Task OnConnectedAsync()
    {
        //достаём id пользователя из JWT-токена (в SignalR он тоже доступен)
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (userId is not null)
        {
            //добавляем в группу user-{id} для персональных уведомлений
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }
        await base.OnConnectedAsync();
    }
}