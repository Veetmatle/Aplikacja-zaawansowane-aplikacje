using ShopApp.Core.Common;
using ShopApp.Core.Enums;

namespace ShopApp.Core.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }

    // Shipping address snapshot
    public string ShippingFirstName { get; set; } = string.Empty;
    public string ShippingLastName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "PL";

    public Guid BuyerId { get; set; }
    public ApplicationUser Buyer { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ItemTitleSnapshot { get; set; } = string.Empty;

    public Guid OrderId { get; set; }
    public Guid ItemId { get; set; }

    public Order Order { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
