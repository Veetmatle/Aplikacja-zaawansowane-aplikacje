using ShopApp.Core.Common;
using ShopApp.Core.Enums;

namespace ShopApp.Core.Entities;

public class Payment : BaseEntity
{
    public Guid OrderId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PLN";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string Provider { get; set; } = "Przelewy24";
    public string? RedirectUrl { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>
    /// Idempotency marker — non-null means notification was already processed.
    /// Prevents double-processing when P24 retries callbacks.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
