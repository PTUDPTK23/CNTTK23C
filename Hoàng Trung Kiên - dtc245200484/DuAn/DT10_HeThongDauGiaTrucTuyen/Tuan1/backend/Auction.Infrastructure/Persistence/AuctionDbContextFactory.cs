using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Auction.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` from the backend folder. Reads the same env var as the API.
/// </summary>
public sealed class AuctionDbContextFactory : IDesignTimeDbContextFactory<AuctionDbContext>
{
    public AuctionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                               ?? "Host=localhost;Port=5433;Database=auctiondb;Username=auction;Password=auction_dev_password";

        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AuctionDbContext(options);
    }
}
