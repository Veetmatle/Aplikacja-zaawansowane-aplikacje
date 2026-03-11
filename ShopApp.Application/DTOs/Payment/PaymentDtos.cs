using ShopApp.Core.Enums;

namespace ShopApp.Application.DTOs.Payment;

public record InitiatePaymentDto(
    Guid OrderId
);

public record PaymentStatusDto(
    Guid PaymentId,
    Guid OrderId,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    string Provider,
    string? RedirectUrl,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record P24NotificationDto(
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
