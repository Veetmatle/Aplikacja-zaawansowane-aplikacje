using ShopApp.Core.Common;

namespace ShopApp.Core.Entities;

public class Cart : BaseEntity
{
    // Null for guest carts (identified by session)
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : BaseEntity
{
    public int Quantity { get; set; } = 1;

    public Guid CartId { get; set; }
    public Guid ItemId { get; set; }

    public Cart Cart { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
