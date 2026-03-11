using Microsoft.AspNetCore.Identity;
using ShopApp.Core.Enums;

namespace ShopApp.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? TimeoutUntil { get; set; }
    public string? BanReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Soft-delete marker. Cannot inherit BaseEntity (IdentityUser constraint),
    /// so DeletedAt is declared directly. GlobalQueryFilter in AppDbContext
    /// automatically excludes soft-deleted users from queries.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt is not null;

    // Navigation properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public Cart? Cart { get; set; }
}
