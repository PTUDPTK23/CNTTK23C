using Auction.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Auction.Tests;

/// <summary>
/// Creates an isolated PostgreSQL database (default: auctiondb_test on port 5433) and applies
/// the real EF Core migrations. Override the connection strings with AUCTION_TEST_PG and
/// AUCTION_TEST_PG_ADMIN when running against another server.
/// </summary>
public sealed class PostgresTestFixture : IAsyncLifetime
{
    public static readonly string AdminConnection =
        Environment.GetEnvironmentVariable("AUCTION_TEST_PG_ADMIN")
        ?? "Host=localhost;Port=5433;Username=postgres;Password=auction_dev_password;Database=postgres";

    public static readonly string TestConnection =
        Environment.GetEnvironmentVariable("AUCTION_TEST_PG")
        ?? "Host=localhost;Port=5433;Username=auction;Password=auction_dev_password;Database=auctiondb_test";

    public bool Available { get; private set; }
    public string? UnavailableReason { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await EnsureDatabaseAsync();

            await using var db = CreateContext();
            await db.Database.MigrateAsync();
            Available = true;
        }
        catch (Exception ex)
        {
            UnavailableReason = ex.Message;
            Available = false;
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AuctionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseNpgsql(TestConnection)
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging()
            .Options;

        return new AuctionDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE outbox_messages, audit_logs, bids, auction_participants, auctions, products, users CASCADE");
    }

    private static async Task EnsureDatabaseAsync()
    {
        var builder = new NpgsqlConnectionStringBuilder(TestConnection);
        var database = builder.Database ?? "auctiondb_test";
        var owner = builder.Username ?? "auction";

        await using var connection = await OpenAdminConnectionAsync();

        await using var exists = connection.CreateCommand();
        exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        exists.Parameters.AddWithValue("name", database);
        if (await exists.ExecuteScalarAsync() is not null)
        {
            return;
        }

        await using var create = connection.CreateCommand();
        create.CommandText = $"CREATE DATABASE {Quote(database)} OWNER {Quote(owner)}";
        await create.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Prefer the configured administrator account; if that role does not exist on the
    /// server (for example the Docker Compose database, where the application role is the
    /// superuser), retry with the application credentials so the suite still runs.
    /// </summary>
    private static async Task<NpgsqlConnection> OpenAdminConnectionAsync()
    {
        try
        {
            var primary = new NpgsqlConnection(AdminConnection);
            await primary.OpenAsync();
            return primary;
        }
        catch (PostgresException)
        {
            var fallbackBuilder = new NpgsqlConnectionStringBuilder(TestConnection)
            {
                Database = "postgres"
            };
            var fallback = new NpgsqlConnection(fallbackBuilder.ConnectionString);
            await fallback.OpenAsync();
            return fallback;
        }
    }

    private static string Quote(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"") + "\"";
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresTestFixture>;
