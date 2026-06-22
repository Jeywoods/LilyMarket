using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LilyMarket.Controllers;

[ApiController]
[Route("api/auctions/{auctionId:guid}/bids")]
[Authorize]  //весь контроллер только для авторизованных
public class BidsController : ControllerBase
{
    private readonly PlaceBidHandler _placeBidHandler;

    public BidsController(PlaceBidHandler placeBidHandler)
    {
        _placeBidHandler = placeBidHandler;
    }

    //POST /api/auctions/{auctionId}/bids — сделать ставку
    //принимает сумму, возвращает результат: успех или ошибку
    [HttpPost]
    public async Task<IActionResult> PlaceBid(Guid auctionId, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        //достаём id того кто ставит из JWT-токена
        var userId = GetUserId();
        var result = await _placeBidHandler.Handle(auctionId, userId, request, ct);
        return Ok(result);
    }

    //вытаскивает id пользователя из JWT-токена
    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        return Guid.Parse(sub!);
    }
}