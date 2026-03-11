using ShopApp.Core.Interfaces.Services;

namespace ShopApp.UnitTests.Mocks;

/// <summary>
/// Fake IPaymentGateway for unit testing — no HTTP calls.
/// Configurable success/failure responses.
/// </summary>
public class FakePaymentGateway : IPaymentGateway
{
    public List<PaymentRequest> InitializedPayments { get; } = new();
    public List<PaymentNotification> VerifiedNotifications { get; } = new();
    public bool ShouldInitSucceed { get; set; } = true;
    public bool ShouldVerificationSucceed { get; set; } = true;

    public Task<PaymentInitResult> InitializePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        InitializedPayments.Add(request);

        var result = ShouldInitSucceed
            ? new PaymentInitResult(true, "https://fake-p24.test/pay/test-token", "test-token")
            : new PaymentInitResult(false, Error: "Fake payment gateway error");

        return Task.FromResult(result);
    }

    public Task<bool> VerifyNotificationAsync(PaymentNotification notification, CancellationToken ct = default)
    {
        VerifiedNotifications.Add(notification);
        return Task.FromResult(ShouldVerificationSucceed);
    }
}
