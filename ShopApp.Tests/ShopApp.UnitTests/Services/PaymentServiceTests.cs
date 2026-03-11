using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ShopApp.Application.DTOs.Payment;
using ShopApp.Application.Services;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Interfaces;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.UnitTests.Mocks;

namespace ShopApp.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly FakePaymentGateway _fakeGateway;
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _paymentRepo = Substitute.For<IPaymentRepository>();
        _orderRepo = Substitute.For<IOrderRepository>();
        _fakeGateway = new FakePaymentGateway();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var configData = new Dictionary<string, string?>
        {
            { "App:BaseUrl", "http://localhost:8080" }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        _sut = new PaymentService(_paymentRepo, _orderRepo, _fakeGateway, _configuration, _unitOfWork);
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepo.GetWithDetailsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var result = await _sut.InitiatePaymentAsync(Guid.NewGuid(), Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenNotOwner_ReturnsForbidden()
    {
        var order = CreateTestOrder();
        _orderRepo.GetWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.InitiatePaymentAsync(order.Id, Guid.NewGuid()); // wrong user

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenAlreadyPaid_ReturnsFailure()
    {
        var order = CreateTestOrder();
        order.PaymentStatus = PaymentStatus.Completed;
        _orderRepo.GetWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.InitiatePaymentAsync(order.Id, order.BuyerId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already paid");
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenValid_CallsGatewayAndReturnsRedirect()
    {
        var order = CreateTestOrder();
        _orderRepo.GetWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((Payment?)null);

        var result = await _sut.InitiatePaymentAsync(order.Id, order.BuyerId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RedirectUrl.Should().Contain("fake-p24");
        _fakeGateway.InitializedPayments.Should().HaveCount(1);
        await _paymentRepo.Received(1).AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiatePaymentAsync_WhenGatewayFails_ReturnsFailure()
    {
        var order = CreateTestOrder();
        _orderRepo.GetWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _paymentRepo.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((Payment?)null);
        _fakeGateway.ShouldInitSucceed = false;

        var result = await _sut.InitiatePaymentAsync(order.Id, order.BuyerId);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Payment initialization failed");
    }

    [Fact]
    public async Task HandleNotificationAsync_WhenSignatureInvalid_ReturnsFailure()
    {
        _fakeGateway.ShouldVerificationSucceed = false;
        var notification = new P24NotificationDto(1, 1, "session", 1000, 1000, "PLN", 1, 1, "stmt", "bad-sign");

        var result = await _sut.HandleNotificationAsync(notification);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid payment notification");
    }

    [Fact]
    public async Task HandleNotificationAsync_WhenValid_MarksPaid()
    {
        _fakeGateway.ShouldVerificationSucceed = true;
        var orderId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            SessionId = "test-session",
            Status = PaymentStatus.Processing
        };
        var order = new Order { Id = orderId, Status = OrderStatus.Pending };

        _paymentRepo.GetBySessionIdAsync("test-session", Arg.Any<CancellationToken>()).Returns(payment);
        _orderRepo.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);

        var notification = new P24NotificationDto(1, 1, "test-session", 1000, 1000, "PLN", 1, 1, "stmt", "sign");
        var result = await _sut.HandleNotificationAsync(notification);

        result.IsSuccess.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.ProcessedAt.Should().NotBeNull();
        order.PaymentStatus.Should().Be(PaymentStatus.Completed);
        order.Status.Should().Be(OrderStatus.Confirmed);
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleNotificationAsync_WhenAlreadyProcessed_ReturnsSuccessWithoutChanges()
    {
        _fakeGateway.ShouldVerificationSucceed = true;
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            SessionId = "test-session",
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _paymentRepo.GetBySessionIdAsync("test-session", Arg.Any<CancellationToken>()).Returns(payment);

        var notification = new P24NotificationDto(1, 1, "test-session", 1000, 1000, "PLN", 1, 1, "stmt", "sign");
        var result = await _sut.HandleNotificationAsync(notification);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().BeginTransactionAsync(Arg.Any<CancellationToken>());
    }

    private static Order CreateTestOrder() => new()
    {
        Id = Guid.NewGuid(),
        BuyerId = Guid.NewGuid(),
        OrderNumber = "ORD-TEST-001",
        TotalAmount = 100m,
        Status = OrderStatus.Pending,
        PaymentStatus = PaymentStatus.Pending,
        Buyer = new ApplicationUser { Email = "test@test.com", FirstName = "Test", LastName = "User" },
        Items = new List<OrderItem>()
    };
}
