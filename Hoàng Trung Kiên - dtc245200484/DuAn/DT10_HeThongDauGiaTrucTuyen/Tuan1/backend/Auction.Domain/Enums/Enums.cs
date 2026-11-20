namespace Auction.Domain.Enums;

public enum UserRole
{
    Bidder = 0,
    Admin = 1
}

public enum AuctionStatus
{
    Scheduled = 0,
    Active = 1,
    Ended = 2,
    Cancelled = 3
}
