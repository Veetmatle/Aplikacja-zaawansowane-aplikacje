using ShopApp.Core.Common;
using ShopApp.Core.Enums;

namespace ShopApp.Core.Entities;

public class Item : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public ItemStatus Status { get; set; } = ItemStatus.Active;
    public ItemCondition Condition { get; set; } = ItemCondition.New;
    public string? Location { get; set; }
    public int ViewCount { get; set; } = 0;
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token — prevents overselling when multiple buyers
    /// attempt to purchase the last item simultaneously.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;

    // Foreign keys
    public Guid CategoryId { get; set; }
    public Guid SellerId { get; set; }

    // Navigation properties
    public Category Category { get; set; } = null!;
    public ApplicationUser Seller { get; set; } = null!;
    public ICollection<ItemPhoto> Photos { get; set; } = new List<ItemPhoto>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
