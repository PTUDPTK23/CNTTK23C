using Auction.Application.Abstractions;
using Auction.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Infrastructure.Persistence;

public sealed class AuctionDbContext : DbContext, IAppDbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AuctionEntity> Auctions => Set<AuctionEntity>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<AuctionParticipant> Participants => Set<AuctionParticipant>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(80).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => x.NormalizedEmail).IsUnique();
            e.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Category).HasMaxLength(60).IsRequired();
            e.Property(x => x.ImageUrl).HasMaxLength(1024);
            e.Property(x => x.StartingPrice).HasPrecision(18, 2);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<AuctionEntity>(e =>
        {
            e.ToTable("auctions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(180).IsRequired();
            e.Property(x => x.StartPrice).HasPrecision(18, 2);
            e.Property(x => x.CurrentPrice).HasPrecision(18, 2);
            e.Property(x => x.MinIncrement).HasPrecision(18, 2);
            e.Property(x => x.WinningAmount).HasPrecision(18, 2);
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.Product).WithMany(p => p.Auctions)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Winner).WithMany()
                .HasForeignKey(x => x.WinnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Status, x.EndTimeUtc });
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => x.WinnerId);
        });

        modelBuilder.Entity<Bid>(e =>
        {
            e.ToTable("bids");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Auction).WithMany(a => a.Bids)
                .HasForeignKey(x => x.AuctionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Bidder).WithMany(u => u.Bids)
                .HasForeignKey(x => x.BidderId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.AuctionId, x.Sequence }).IsUnique();
            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => new { x.AuctionId, x.Amount }).IsDescending(false, true);
            e.HasIndex(x => x.PlacedAtUtc);
        });

        modelBuilder.Entity<AuctionParticipant>(e =>
        {
            e.ToTable("auction_participants");
            e.HasKey(x => new { x.AuctionId, x.UserId });
            e.HasOne(x => x.Auction).WithMany(a => a.Participants)
                .HasForeignKey(x => x.AuctionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany(u => u.Participations)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(60).IsRequired();
            e.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.Source).HasMaxLength(40);
            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => x.OccurredAtUtc).IsDescending();
            e.HasIndex(x => x.EventType);
            e.HasIndex(x => x.AuctionId);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(60).IsRequired();
            e.Property(x => x.RoutingKey).HasMaxLength(120).IsRequired();
            e.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            e.Property(x => x.LastError).HasMaxLength(2000);
            e.HasIndex(x => x.PublishedAtUtc);
        });
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken);

    public Task LockAuctionAsync(Guid auctionId, CancellationToken cancellationToken = default) =>
        Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM auctions WHERE id = {auctionId} FOR UPDATE", cancellationToken);
}
