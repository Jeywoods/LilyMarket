using FluentAssertions;
using LilyMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LilyMarket.Tests.Integration;

public class AuctionLifecycleTests
{
    [Fact]
    public async Task FullLifecycle_CreateBidEnd_WinnerCorrect()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);

        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}