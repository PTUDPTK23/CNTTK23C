using Auction.Application.Abstractions;
using Auction.Application.Common;
using Auction.Application.Dtos;
using Auction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auction.Application.Services;

public sealed class ProductService
{
    private readonly IAppDbContext _db;
    private readonly IClock _clock;

    public ProductService(IAppDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<ProductDto>> ListAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        (page, pageSize) = Pagination.Normalize(page, pageSize);
        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Category == category);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.Category, p.ImageUrl, p.StartingPrice,
                p.CreatedAtUtc, p.UpdatedAtUtc, p.Auctions.Count))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, page, pageSize, total);
    }

    public async Task<ProductDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(
                p.Id, p.Name, p.Description, p.Category, p.ImageUrl, p.StartingPrice,
                p.CreatedAtUtc, p.UpdatedAtUtc, p.Auctions.Count))
            .FirstOrDefaultAsync(ct);

        return product ?? throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            StartingPrice = Domain.Rules.BidRules.RoundMoney(request.StartingPrice),
            CreatedAtUtc = _clock.UtcNow,
            UpdatedAtUtc = _clock.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return new ProductDto(product.Id, product.Name, product.Description, product.Category,
            product.ImageUrl, product.StartingPrice, product.CreatedAtUtc, product.UpdatedAtUtc, 0);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Category = request.Category.Trim();
        product.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        product.StartingPrice = Domain.Rules.BidRules.RoundMoney(request.StartingPrice);
        product.UpdatedAtUtc = _clock.UtcNow;

        await _db.SaveChangesAsync(ct);

        var auctionCount = await _db.Auctions.CountAsync(a => a.ProductId == id, ct);
        return new ProductDto(product.Id, product.Name, product.Description, product.Category,
            product.ImageUrl, product.StartingPrice, product.CreatedAtUtc, product.UpdatedAtUtc, auctionCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                      ?? throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");

        var hasAuctions = await _db.Auctions.AnyAsync(a => a.ProductId == id, ct);
        if (hasAuctions)
        {
            throw AppException.Conflict("PRODUCT_IN_USE",
                "Sản phẩm đã gắn với một hoặc nhiều phiên đấu giá nên không thể xóa.");
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        await _db.Products.AsNoTracking()
            .Where(p => p.Category != "")
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
}
