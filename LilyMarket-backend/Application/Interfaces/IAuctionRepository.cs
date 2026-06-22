using LilyMarket.Application.Common;
using LilyMarket.Domain.Entities;

namespace LilyMarket.Application.Interfaces;

public interface IAuctionRepository
{
    Task<Auction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Auction?> GetByIdWithBidsAsync(Guid id, CancellationToken ct = default);
    //метод с блокировкой строки для конкурентных операций (ставки)
    Task<Auction?> GetByIdWithBidsForUpdateAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Auction>> GetExpiredActiveAuctionsAsync(DateTime now, CancellationToken ct = default);
    Task<PagedResult<Auction>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Auction auction, CancellationToken ct = default);
    void Update(Auction auction);
}