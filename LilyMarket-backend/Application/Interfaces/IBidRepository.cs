using LilyMarket.Domain.Entities;

namespace LilyMarket.Application.Interfaces;

public interface IBidRepository
{
    Task AddAsync(Bid bid, CancellationToken ct = default);
    Task<IEnumerable<Bid>> GetByAuctionIdAsync(Guid auctionId, CancellationToken ct = default);
}