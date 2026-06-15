using LilyMarket.Application.Interfaces;
using LilyMarket.Domain.Entities;
using LilyMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LilyMarket.Infrastructure.Repositories;

public class BidRepository : IBidRepository
{
    private readonly AppDbContext _context;

    public BidRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Bid bid, CancellationToken ct = default)
    {
        await _context.Bids.AddAsync(bid, ct);
    }

    public async Task<IEnumerable<Bid>> GetByAuctionIdAsync(Guid auctionId, CancellationToken ct = default)
    {
        return await _context.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.PlacedAt)
            .ToListAsync(ct);
    }
}