using ShopApp.Core.Common;

namespace ShopApp.Core.Entities;

public class ItemPhoto : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; } = false;
    public int Order { get; set; } = 0;

    public Guid ItemId { get; set; }
    public Item Item { get; set; } = null!;
}
