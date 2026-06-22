using LilyMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LilyMarket.Tests.Integration;

public class TestDatabaseFixture : IDisposable
{
    private const string ConnectionString = "Host=localhost;Port=5433;Database=lilymarket_test;Username=postgres;Password=postgres";

    public TestDatabaseFixture()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options);
    }

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}