namespace ShopApp.Core.Interfaces.Services;

/// <summary>
/// Abstraction for payment gateway (Przelewy24, Stripe, etc.)
/// Infrastructure provides the real implementation.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentInitResult> InitializePaymentAsync(PaymentRequest request, CancellationToken ct = default);
    Task<bool> VerifyNotificationAsync(PaymentNotification notification, CancellationToken ct = default);
}

/// <summary>Request to initialize a payment session.</summary>
public record PaymentRequest(
    Guid OrderId,
    string SessionId,
    decimal Amount,
    string Currency,
    string Description,
    string CustomerEmail,
    string ReturnUrl,
    string NotifyUrl
);

/// <summary>Result of payment initialization — redirect URL for customer.</summary>
public record PaymentInitResult(
    bool Success,
    string? RedirectUrl = null,
    string? TransactionId = null,
    string? Error = null
);

/// <summary>Notification from P24 about payment status.</summary>
public record PaymentNotification(
    int MerchantId,
    int PosId,
    string SessionId,
    int Amount,
    int OriginAmount,
    string Currency,
    int OrderId,
    int MethodId,
    string Statement,
    string Sign
);
