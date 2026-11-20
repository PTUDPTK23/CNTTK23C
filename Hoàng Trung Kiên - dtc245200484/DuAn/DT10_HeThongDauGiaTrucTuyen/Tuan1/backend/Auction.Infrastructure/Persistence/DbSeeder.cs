using Auction.Application.Abstractions;
using Auction.Domain.Entities;
using Auction.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AuctionEntity = Auction.Domain.Entities.Auction;

namespace Auction.Infrastructure.Persistence;

/// <summary>Idempotent demo data so the UI and realtime flow are usable immediately.</summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@auction.local";
    public const string AdminPassword = "Admin@123";
    public const string BidderPassword = "Bidder@123";

    public static async Task SeedAsync(
        AuctionDbContext db,
        IPasswordHasher hasher,
        ILogger logger,
        CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            logger.LogInformation("Bỏ qua tạo dữ liệu mẫu: tài khoản đã tồn tại.");
            return;
        }

        var now = DateTime.UtcNow;
        var admin = NewUser("admin@auction.local", "Nguyễn Hoàng Nam", AdminPassword, UserRole.Admin, now, hasher);
        var alice = NewUser("alice@auction.local", "Nguyễn Minh Anh", BidderPassword, UserRole.Bidder, now, hasher);
        var bob = NewUser("bob@auction.local", "Trần Quốc Bảo", BidderPassword, UserRole.Bidder, now, hasher);
        var chloe = NewUser("chloe@auction.local", "Lê Thu Hà", BidderPassword, UserRole.Bidder, now, hasher);

        db.Users.AddRange(admin, alice, bob, chloe);

        var products = new[]
        {
            NewProduct("Đồng hồ Chronograph Sapphire Đêm",
                "Đồng hồ chronograph tự động hoàn thiện thủ công với mặt số xanh sapphire, kính sapphire và vỏ vàng trắng 18k đánh số. Kèm hộp, chứng thư và hồ sơ bảo dưỡng năm 1968.",
                "Đồng hồ", "/media/watch.svg", 12000m, now),
            NewProduct("Tượng Obsidian — 'Cực Quang'",
                "Tác phẩm điêu khắc obsidian và đồng phong hóa độc bản của atelier Mirova. Có chữ ký trên đế, từng trưng bày tại triển lãm thiết kế Venice 2024.",
                "Nghệ thuật", "/media/sculpture.svg", 6400m, now),
            NewProduct("Xe GT Coupé 1967",
                "Xe grand tourer số máy trùng khớp, phục chế đạt chuẩn concours. Động cơ V12 4.2L, hộp số sàn năm cấp, lịch sử sở hữu đầy đủ từ khi xuất xưởng. Kèm hồ sơ phục chế toàn bộ.",
                "Xe cộ", "/media/car.svg", 85000m, now),
            NewProduct("Nhẫn Ngọc Lục Bảo Hoàng Gia",
                "Ngọc lục bảo Colombia 4.8 carat nạm bạch kim cùng kim cương giác cắt baguette. Kèm giấy giám định SSEF và GIA.",
                "Trang sức", "/media/ring.svg", 8000m, now),
            NewProduct("Bản thảo sơ bản, 1897",
                "Các trang bản thảo gốc có sửa tay, đóng bìa da Morocco chạm khắc mạ vàng. Nguồn gốc: bộ sưu tập tư nhân châu Âu.",
                "Sách", "/media/manuscript.svg", 3200m, now),
            NewProduct("Synth Analog Cổ Điển",
                "Tổng hợp âm đa âm dạng module sản xuất năm 1979, đã bảo dưỡng toàn bộ. Vỏ gỗ óc chó nguyên bản, dao động đã hiệu chuẩn và nguồn điện mới.",
                "Âm nhạc", "/media/synth.svg", 5200m, now),
            NewProduct("Ghế Lounge Aurelian",
                "Ghế lounge điêu khắc bằng gỗ óc chó nguyên khối và da bò cognac khâu tay. Phiên bản giới hạn mười hai chiếc, đây là chiếc số bốn.",
                "Nội thất", "/media/chair.svg", 2400m, now),
            NewProduct("Mảnh Thiên Thạch Mặt Trăng",
                "Mảnh thiên thạch mặt trăng có định hướng nặng 412 gram với lớp vỏ nóng chảy tự nhiên, phân loại NWA 13788. Kèm giá trưng bày acrylic và giấy chứng nhận phòng thí nghiệm.",
                "Sưu tầm", "/media/meteorite.svg", 4200m, now),
            NewProduct("Katana Nhật Bản, Thời Edo",
                "Katana thời Edo có chữ ký, kèm giấy chứng nhận NBTHK Hozon, bộ koshirae nguyên bản và hộp bảo quản lót lụa.",
                "Sưu tầm", "/media/katana.svg", 12000m, now),
            NewProduct("Dây Chuyền Kim Cương Art Deco",
                "Dây chuyền bạch kim phong cách Art Deco nạm kim cương cắt châu Âu cổ với tổng trọng lượng 12.6 carat, khoảng năm 1925.",
                "Trang sức", "/media/necklace.svg", 18000m, now)
        };

        db.Products.AddRange(products);

        var watchAuction = NewAuction(products[0], "Chronograph Sapphire Đêm — Số 07",
            12000m, 500m, now.AddHours(-20), now.AddMinutes(45), admin, now);
        var artAuction = NewAuction(products[1], "Obsidian 'Cực Quang' — Bản đúc duy nhất",
            6400m, 250m, now.AddHours(-6), now.AddHours(6), admin, now);
        var carAuction = NewAuction(products[2], "GT Coupé 1967 — Phục chế hoàn hảo",
            85000m, 2500m, now.AddHours(-2), now.AddDays(2), admin, now);
        var katanaAuction = NewAuction(products[8], "Katana Edo kèm giấy NBTHK",
            12000m, 500m, now.AddHours(-30), now.AddDays(1), admin, now);
        var manuscriptAuction = NewAuction(products[4], "Bản thảo 1897 — Bộ sưu tập tư nhân",
            3200m, 100m, now.AddHours(2), now.AddDays(3), admin, now);
        var ringAuction = NewAuction(products[3], "Nhẫn Ngọc Lục Bảo Hoàng Gia — 4.8ct",
            8000m, 500m, now.AddDays(-3), now.AddHours(-2), admin, now);

        db.Auctions.AddRange(watchAuction, artAuction, carAuction, katanaAuction, manuscriptAuction, ringAuction);

        var bids = new List<Bid>();
        AddBid(bids, watchAuction, bob, 13500m, now.AddMinutes(-32));
        AddBid(bids, watchAuction, alice, 14200m, now.AddMinutes(-21));
        AddBid(bids, watchAuction, chloe, 14800m, now.AddMinutes(-12));
        AddBid(bids, watchAuction, bob, 15500m, now.AddMinutes(-4));

        AddBid(bids, artAuction, alice, 6900m, now.AddMinutes(-40));
        AddBid(bids, artAuction, chloe, 7400m, now.AddMinutes(-9));

        AddBid(bids, katanaAuction, alice, 13000m, now.AddMinutes(-90));

        AddBid(bids, ringAuction, bob, 8500m, now.AddDays(-1));
        AddBid(bids, ringAuction, alice, 9000m, now.AddDays(-1).AddMinutes(5));
        AddBid(bids, ringAuction, bob, 9600m, now.AddDays(-1).AddMinutes(10));
        AddBid(bids, ringAuction, alice, 10200m, now.AddDays(-1).AddMinutes(15));

        watchAuction.CurrentPrice = 15500m;
        artAuction.CurrentPrice = 7400m;
        katanaAuction.CurrentPrice = 13000m;
        ringAuction.CurrentPrice = 10200m;
        ringAuction.Status = AuctionStatus.Ended;
        var winningRingBid = bids.Last(b => b.AuctionId == ringAuction.Id);
        winningRingBid.IsWinning = true;
        ringAuction.WinnerId = alice.Id;
        ringAuction.WinningBidId = winningRingBid.Id;
        ringAuction.WinningAmount = 10200m;
        ringAuction.EndedAtUtc = now.AddHours(-2);

        db.Bids.AddRange(bids);
        db.Participants.AddRange(BuildParticipants(bids, now));

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Đã tạo dữ liệu mẫu: {Users} tài khoản, {Products} sản phẩm, {Auctions} phiên đấu giá, {Bids} lượt đặt giá.",
            4, products.Length, 6, bids.Count);
    }

    private static User NewUser(string email, string name, string password, UserRole role, DateTime now,
        IPasswordHasher hasher) => new()
    {
        Email = email,
        NormalizedEmail = email.ToLowerInvariant(),
        DisplayName = name,
        PasswordHash = hasher.Hash(password),
        Role = role,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static Product NewProduct(string name, string description, string category, string imageUrl,
        decimal startingPrice, DateTime now) => new()
    {
        Name = name,
        Description = description,
        Category = category,
        ImageUrl = imageUrl,
        StartingPrice = startingPrice,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static AuctionEntity NewAuction(Product product, string title, decimal startPrice, decimal increment,
        DateTime start, DateTime end, User admin, DateTime now) => new()
    {
        ProductId = product.Id,
        Title = title,
        StartPrice = startPrice,
        CurrentPrice = startPrice,
        MinIncrement = increment,
        StartTimeUtc = start,
        EndTimeUtc = end,
        Status = start <= now && end > now ? AuctionStatus.Active : end <= now ? AuctionStatus.Ended : AuctionStatus.Scheduled,
        CreatedByUserId = admin.Id,
        CreatedAtUtc = now,
        UpdatedAtUtc = now
    };

    private static void AddBid(List<Bid> bids, AuctionEntity auction, User bidder, decimal amount, DateTime placedAt)
    {
        var sequence = bids.Count(b => b.AuctionId == auction.Id) + 1;
        bids.Add(new Bid
        {
            AuctionId = auction.Id,
            BidderId = bidder.Id,
            Amount = amount,
            Sequence = sequence,
            PlacedAtUtc = DateTime.SpecifyKind(placedAt, DateTimeKind.Utc),
            IsWinning = false
        });
    }

    private static IEnumerable<AuctionParticipant> BuildParticipants(List<Bid> bids, DateTime now) =>
        bids.GroupBy(b => new { b.AuctionId, b.BidderId })
            .Select(g => new AuctionParticipant
            {
                AuctionId = g.Key.AuctionId,
                UserId = g.Key.BidderId,
                JoinedAtUtc = g.Min(b => b.PlacedAtUtc),
                LastBidAtUtc = g.Max(b => b.PlacedAtUtc),
                BidCount = g.Count()
            });
}
