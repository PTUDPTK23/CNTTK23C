using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Domain.Entities;
using Auction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Application.Services;

public sealed class AuctionService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public AuctionService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<AuctionSummaryDto>> ListAsync(
        string? status,
        string? category,
        string? search,
        string? sort,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, 12);
        var now = _clock.UtcNow;
        var query = _db.Auctions.AsNoTracking().AsQueryable();

        var normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
        query = normalizedStatus switch
        {
            "active" => query.Where(a => a.Status == AuctionStatus.Active && a.EndTimeUtc > now),
            "scheduled" or "upcoming" => query.Where(a => a.Status == AuctionStatus.Scheduled && a.EndTimeUtc > now),
            "ended" => query.Where(a => a.Status == AuctionStatus.Ended),
            "cancelled" => query.Where(a => a.Status == AuctionStatus.Cancelled),
            _ => query.Where(a => a.Status != AuctionStatus.Cancelled)
        };

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.Product.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(term) || a.Product.Name.ToLower().Contains(term));
        }

        query = (sort ?? "ending").Trim().ToLowerInvariant() switch
        {
            "newest" => query.OrderByDescending(a => a.CreatedAtUtc),
            "price" or "price-desc" => query.OrderByDescending(a => a.CurrentPrice),
            "price-asc" => query.OrderBy(a => a.CurrentPrice),
            _ => query.OrderBy(a => a.Status == AuctionStatus.Active ? 0 : 1)
                .ThenBy(a => a.EndTimeUtc)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(AuctionQueries.Summary(now))
            .ToListAsync(ct);

        return new PagedResult<AuctionSummaryDto>(items, page, pageSize, total);
    }

    public async Task<AuctionStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var totalAuctions = await _db.Auctions.CountAsync(a => a.Status != AuctionStatus.Cancelled, ct);
        var activeAuctions = await _db.Auctions.CountAsync(
            a => a.Status == AuctionStatus.Active && a.EndTimeUtc > now, ct);
        var totalBids = await _db.Bids.CountAsync(ct);
        var totalBidders = await _db.Users.CountAsync(u => u.Role == UserRole.Bidder, ct);

        return new AuctionStatsDto(totalAuctions, activeAuctions, totalBids, totalBidders);
    }

    public async Task<AuctionDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var auction = await _db.Auctions.AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.Winner)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw AppException.NotFound("AUCTION_NOT_FOUND", "Không tìm thấy phiên đấu giá.");

        var sellerName = await _db.Users.AsNoTracking()
            .Where(u => u.Id == auction.CreatedByUserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(ct) ?? "Nhà đấu giá";

        var recentBids = await _db.Bids.AsNoTracking()
            .Where(b => b.AuctionId == id)
            .OrderByDescending(b => b.PlacedAtUtc)
            .Take(20)
            .Select(b => new BidDto(b.Id, b.AuctionId, null, b.Amount, b.BidderId, b.Bidder.DisplayName,
                b.PlacedAtUtc, b.Sequence, b.IsWinning))
            .ToListAsync(ct);

        var bidCount = await _db.Bids.CountAsync(b => b.AuctionId == id, ct);
        var participantCount = await _db.Participants.CountAsync(p => p.AuctionId == id, ct);
        var highestBidder = recentBids.OrderByDescending(b => b.Amount).FirstOrDefault()?.BidderName;

        WinnerDto? winner = auction.Winner is null ? null : new WinnerDto(
            auction.WinnerId!.Value,
            auction.Winner.DisplayName,
            auction.WinningAmount ?? auction.CurrentPrice,
            auction.WinningBidId);

        return new AuctionDetailDto(
            auction.Id,
            auction.Title,
            auction.ProductId,
            auction.Product.Name,
            auction.Product.Category,
            auction.Product.ImageUrl,
            auction.Product.Description,
            auction.StartPrice,
            auction.CurrentPrice,
            auction.MinIncrement,
            auction.CurrentPrice + auction.MinIncrement,
            auction.StartTimeUtc,
            auction.EndTimeUtc,
            auction.Status.ToApi(),
            bidCount,
            participantCount,
            highestBidder,
            sellerName,
            auction.CreatedAtUtc,
            auction.EndedAtUtc,
            winner,
            recentBids,
            now);
    }

    public async Task<AuctionDetailDto> CreateAsync(CreateAuctionRequest request, Guid adminUserId, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var start = NormalizeUtc(request.StartTimeUtc);
        var end = NormalizeUtc(request.EndTimeUtc);

        if (start == default || end == default)
        {
            throw AppException.BadRequest("INVALID_TIME_RANGE", "Cần nhập thời gian mở và kết thúc phiên (UTC).");
        }

        if (end <= start)
        {
            throw AppException.BadRequest("INVALID_TIME_RANGE", "Thời gian kết thúc phải sau thời gian mở phiên.");
        }

        if (end <= now)
        {
            throw AppException.BadRequest("INVALID_TIME_RANGE", "Thời gian kết thúc phải ở tương lai.");
        }

        if (end - start > Domain.Rules.BidRules.MaxBidWindow)
        {
            throw AppException.BadRequest("INVALID_TIME_RANGE", "Một phiên đấu giá không được kéo dài quá 90 ngày.");
        }

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
                      ?? throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");

        var startPrice = Domain.Rules.BidRules.RoundMoney(request.StartPrice > 0 ? request.StartPrice : product.StartingPrice);
        var increment = Domain.Rules.BidRules.RoundMoney(request.MinIncrement);

        if (startPrice <= 0 || increment <= 0)
        {
            throw AppException.BadRequest("INVALID_AMOUNT", "Giá khởi điểm và bước giá tối thiểu phải lớn hơn 0.");
        }

        var auction = new AuctionEntity
        {
            ProductId = product.Id,
            Title = request.Title.Trim(),
            StartPrice = startPrice,
            CurrentPrice = startPrice,
            MinIncrement = increment,
            StartTimeUtc = start,
            EndTimeUtc = end,
            Status = start <= now ? AuctionStatus.Active : AuctionStatus.Scheduled,
            CreatedByUserId = adminUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.Auctions.Add(auction);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(auction.Id, ct);
    }

    public async Task<PagedResult<BidDto>> GetBidsAsync(Guid auctionId, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, 25);
        var exists = await _db.Auctions.AnyAsync(a => a.Id == auctionId, ct);
        if (!exists)
        {
            throw AppException.NotFound("AUCTION_NOT_FOUND", "Không tìm thấy phiên đấu giá.");
        }

        var query = _db.Bids.AsNoTracking().Where(b => b.AuctionId == auctionId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(b => b.PlacedAtUtc)
            .ThenByDescending(b => b.Sequence)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BidDto(b.Id, b.AuctionId, null, b.Amount, b.BidderId, b.Bidder.DisplayName,
                b.PlacedAtUtc, b.Sequence, b.IsWinning))
            .ToListAsync(ct);

        return new PagedResult<BidDto>(items, page, pageSize, total);
    }

    public async Task<AuctionResultDto> GetResultAsync(Guid auctionId, CancellationToken ct = default)
    {
        var auction = await _db.Auctions.AsNoTracking()
            .Include(a => a.Winner)
            .FirstOrDefaultAsync(a => a.Id == auctionId, ct)
            ?? throw AppException.NotFound("AUCTION_NOT_FOUND", "Không tìm thấy phiên đấu giá.");

        var totalBids = await _db.Bids.CountAsync(b => b.AuctionId == auctionId, ct);
        var hasWinner = auction.Status == AuctionStatus.Ended && auction.WinnerId is not null;

        return new AuctionResultDto(
            auction.Id,
            auction.Title,
            auction.Status.ToApi(),
            hasWinner,
            hasWinner ? auction.WinnerId : null,
            hasWinner ? auction.Winner?.DisplayName : null,
            hasWinner ? auction.WinningAmount : null,
            hasWinner ? auction.WinningBidId : null,
            totalBids,
            auction.EndedAtUtc);
    }

    public async Task<IReadOnlyList<ParticipantDto>> GetParticipantsAsync(Guid auctionId, CancellationToken ct = default)
    {
        var exists = await _db.Auctions.AnyAsync(a => a.Id == auctionId, ct);
        if (!exists)
        {
            throw AppException.NotFound("AUCTION_NOT_FOUND", "Không tìm thấy phiên đấu giá.");
        }

        return await _db.Participants.AsNoTracking()
            .Where(p => p.AuctionId == auctionId)
            .OrderByDescending(p => p.BidCount)
            .Select(p => new ParticipantDto(p.UserId, p.User.DisplayName, p.JoinedAtUtc, p.LastBidAtUtc, p.BidCount))
            .ToListAsync(ct);
    }

    public static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
