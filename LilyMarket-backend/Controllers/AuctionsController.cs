using LilyMarket.Application.DTO.Auctions;
using LilyMarket.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LilyMarket.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
    private readonly GetAuctionsHandler _getAuctionsHandler;
    private readonly GetAuctionByIdHandler _getAuctionByIdHandler;
    private readonly CreateAuctionHandler _createAuctionHandler;
    private readonly UpdateAuctionHandler _updateAuctionHandler;
    private readonly CancelAuctionHandler _cancelAuctionHandler;

    public AuctionsController(
        GetAuctionsHandler getAuctionsHandler,
        GetAuctionByIdHandler getAuctionByIdHandler,
        CreateAuctionHandler createAuctionHandler,
        UpdateAuctionHandler updateAuctionHandler,
        CancelAuctionHandler cancelAuctionHandler)
    {
        _getAuctionsHandler = getAuctionsHandler;
        _getAuctionByIdHandler = getAuctionByIdHandler;
        _createAuctionHandler = createAuctionHandler;
        _updateAuctionHandler = updateAuctionHandler;
        _cancelAuctionHandler = cancelAuctionHandler;
    }

    //GET /api/auctions?page=1&pageSize=20 — список аукционов с пагинацией
    //доступно всем, даже без авторизации
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        //ограничиваем максимум 50 на странице — защита от перегрузки
        if (pageSize > 50)
            pageSize = 50;

        var result = await _getAuctionsHandler.Handle(page, pageSize, ct);
        return Ok(result);
    }

    //GET /api/auctions/{id} — детальная информация об аукционе
    //включает историю последних 5 ставок
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _getAuctionByIdHandler.Handle(id, ct);
        return Ok(result);
    }

    //POST /api/auctions — создать новый аукцион
    //только для авторизованных пользователей
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateAuctionRequest request, CancellationToken ct)
    {
        //достаём id пользователя из JWT-токена
        var userId = GetUserId();
        var result = await _createAuctionHandler.Handle(userId, request, ct);
        //201 Created + ссылка на созданный аукцион
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    //PUT /api/auctions/{id} — изменить аукцион
    //только продавец и только до первой ставки
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAuctionRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _updateAuctionHandler.Handle(id, userId, request, ct);
        return Ok(result);
    }

    //DELETE /api/auctions/{id} — отменить аукцион
    //только продавец и только до первой ставки
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        await _cancelAuctionHandler.Handle(id, userId, ct);
        //204 No Content — успешно отменён
        return NoContent();
    }

    //вытаскивает id пользователя из JWT-токена
    //пробует два формата claim: стандартный "sub" и старый формат
    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        return Guid.Parse(sub!);
    }
}