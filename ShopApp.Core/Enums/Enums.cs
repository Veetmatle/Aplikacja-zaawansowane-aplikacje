namespace ShopApp.Core.Enums;

public enum UserStatus
{
    Active = 0,
    Banned = 1,
    TimedOut = 2
}

public enum ItemStatus
{
    Active = 0,
    Inactive = 1,
    Sold = 2,
    Removed = 3
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    Refunded = 5
}

public enum ItemCondition
{
    New = 0,
    Used = 1,
    Refurbished = 2
}
