using Microsoft.Extensions.Configuration;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Payment;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Interfaces;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.Application.Services;

/// <summary>
/// Orchestrates the payment flow:
/// 1. InitiatePayment — creates Payment entity, calls IPaymentGateway, returns redirect URL.
/// 2. HandleNotification — P24 callback, verifies signature, marks order as paid (transactional + idempotent).
/// 3. GetPaymentStatus — check current payment status for an order.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IPaymentGateway paymentGateway,
        IConfiguration configuration,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _paymentGateway = paymentGateway;
        _configuration = configuration;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentStatusDto>> InitiatePaymentAsync(Guid orderId, Guid userId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetWithDetailsAsync(orderId, ct);
        if (order is null)
            return Result<PaymentStatusDto>.NotFound("Order not found.");

        if (order.BuyerId != userId)
            return Result<PaymentStatusDto>.Forbidden("You can only pay for your own orders.");

        if (order.PaymentStatus == PaymentStatus.Completed)
            return Result<PaymentStatusDto>.Failure("Order is already paid.");

        // Check for existing pending payment
        var existingPayment = await _paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (existingPayment is not null && existingPayment.Status == PaymentStatus.Pending)
        {
            // Return existing redirect URL
            return Result<PaymentStatusDto>.Success(MapToDto(existingPayment));
        }

        var sessionId = $"order-{orderId:N}-{DateTime.UtcNow.Ticks}";
        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:8080";

        var paymentRequest = new PaymentRequest(
            OrderId: orderId,
            SessionId: sessionId,
            Amount: order.TotalAmount,
            Currency: "PLN",
            Description: $"ShopApp Order {order.OrderNumber}",
            CustomerEmail: order.Buyer?.Email ?? "customer@shopapp.local",
            ReturnUrl: $"{baseUrl}/api/payments/return?orderId={orderId}",
            NotifyUrl: $"{baseUrl}/api/payments/notify"
        );

        var initResult = await _paymentGateway.InitializePaymentAsync(paymentRequest, ct);

        var payment = new Payment
        {
            OrderId = orderId,
            SessionId = sessionId,
            Amount = order.TotalAmount,
            Currency = "PLN",
            Status = initResult.Success ? PaymentStatus.Processing : PaymentStatus.Failed,
            RedirectUrl = initResult.RedirectUrl,
            TransactionId = initResult.TransactionId,
            FailureReason = initResult.Error,
        };

        await _paymentRepository.AddAsync(payment, ct);

        if (!initResult.Success)
            return Result<PaymentStatusDto>.Failure($"Payment initialization failed: {initResult.Error}");

        order.PaymentStatus = PaymentStatus.Processing;
        await _orderRepository.UpdateAsync(order, ct);

        return Result<PaymentStatusDto>.Success(MapToDto(payment));
    }

    public async Task<Result> HandleNotificationAsync(P24NotificationDto notification, CancellationToken ct = default)
    {
        // 1. Verify signature before any DB changes
        var gatewayNotification = new PaymentNotification(
            MerchantId: notification.MerchantId,
            PosId: notification.PosId,
            SessionId: notification.SessionId,
            Amount: notification.Amount,
            OriginAmount: notification.OriginAmount,
            Currency: notification.Currency,
            OrderId: notification.OrderId,
            MethodId: notification.MethodId,
            Statement: notification.Statement,
            Sign: notification.Sign
        );

        var isValid = await _paymentGateway.VerifyNotificationAsync(gatewayNotification, ct);
        if (!isValid)
            return Result.Failure("Invalid payment notification signature.");

        // 2. Find payment by session ID
        var payment = await _paymentRepository.GetBySessionIdAsync(notification.SessionId, ct);
        if (payment is null)
            return Result.Failure("Payment not found for given session.");

        // 3. Idempotency — already processed (check ProcessedAt, not just Status)
        if (payment.ProcessedAt is not null)
            return Result.Success();

        // 4. Transactional update: Payment + Order atomically
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            payment.Status = PaymentStatus.Completed;
            payment.CompletedAt = DateTime.UtcNow;
            payment.ProcessedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment, ct);

            // 5. Update order
            var order = await _orderRepository.GetByIdAsync(payment.OrderId, ct);
            if (order is not null)
            {
                order.PaymentStatus = PaymentStatus.Completed;
                order.Status = OrderStatus.Confirmed;
                await _orderRepository.UpdateAsync(order, ct);
            }

            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        return Result.Success();
    }

    public async Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(Guid orderId, Guid userId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
            return Result<PaymentStatusDto>.NotFound("Order not found.");

        if (order.BuyerId != userId)
            return Result<PaymentStatusDto>.Forbidden();

        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, ct);
        if (payment is null)
            return Result<PaymentStatusDto>.NotFound("No payment found for this order.");

        return Result<PaymentStatusDto>.Success(MapToDto(payment));
    }

    private static PaymentStatusDto MapToDto(Payment payment) => new(
        PaymentId: payment.Id,
        OrderId: payment.OrderId,
        Status: payment.Status,
        Amount: payment.Amount,
        Currency: payment.Currency,
        Provider: payment.Provider,
        RedirectUrl: payment.RedirectUrl,
        CreatedAt: payment.CreatedAt,
        CompletedAt: payment.CompletedAt
    );
}
