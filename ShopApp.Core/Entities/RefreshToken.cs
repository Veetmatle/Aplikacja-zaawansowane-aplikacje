using ShopApp.Core.Common;

namespace ShopApp.Core.Entities;

public class RefreshToken : BaseEntity
{
    /// <summary>SHA-256 hash of the token value — never store plaintext.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? RevokeReason { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
