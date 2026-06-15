using LilyMarket.Application.Common;
using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Domain.Enums;
using LilyMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LilyMarket.Infrastructure.Repositories;

public class AuctionRepository : IAuctionRepository
{
    private readonly AppDbContext _context;

    public AuctionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Auction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Auctions.FindAsync(new object[] { id }, ct);
    }

    public async Task<Auction?> GetByIdWithBidsAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Auctions
            .Include(a => a.Bids)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<IEnumerable<Auction>> GetExpiredActiveAuctionsAsync(DateTime now, CancellationToken ct = default)
    {
        return await _context.Auctions
            .Include(a => a.Bids)
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Auction>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Auctions
            .Include(a => a.Bids)
            .OrderByDescending(a => a.StartedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Auction>(items, totalCount, page, pageSize);
    }

    public async Task AddAsync(Auction auction, CancellationToken ct = default)
    {
        await _context.Auctions.AddAsync(auction, ct);
    }

    public void Update(Auction auction)
    {
        _context.Auctions.Update(auction);
    }
}