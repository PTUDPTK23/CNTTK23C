using Auction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auction.Application.Abstractions;

/// <summary>
/// Persistence abstraction implemented by Auction.Infrastructure (EF Core + PostgreSQL).
/// Application services only depend on this interface.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    DbSet<Domain.Entities.Auction> Auctions { get; }
    DbSet<Bid> Bids { get; }
    DbSet<AuctionParticipant> Participants { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Locks the auction row (SELECT ... FOR UPDATE) inside the current transaction.</summary>
    Task LockAuctionAsync(Guid auctionId, CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
