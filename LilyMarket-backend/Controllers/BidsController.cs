using LilyMarket.Application.DTO.Bids;
using LilyMarket.Application.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LilyMarket.Controllers;

[ApiController]
[Route("api/auctions/{auctionId:guid}/bids")]
[Authorize]
public class BidsController : ControllerBase
{
    private readonly PlaceBidHandler _placeBidHandler;

    public BidsController(PlaceBidHandler placeBidHandler)
    {
        _placeBidHandler = placeBidHandler;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceBid(Guid auctionId, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _placeBidHandler.Handle(auctionId, userId, request, ct);
        return Ok(result);
    }

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        return Guid.Parse(sub!);
    }
}