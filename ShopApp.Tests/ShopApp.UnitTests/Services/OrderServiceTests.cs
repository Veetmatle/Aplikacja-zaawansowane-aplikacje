using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Services;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Exceptions;
using ShopApp.Core.Interfaces;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.UnitTests.Services;

public class OrderServiceTests
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IItemRepository _itemRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _orderRepo = Substitute.For<IOrderRepository>();
        _cartRepo = Substitute.For<ICartRepository>();
        _itemRepo = Substitute.For<IItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _sut = new OrderService(_orderRepo, _cartRepo, _itemRepo, _unitOfWork);
    }

    [Fact]
    public async Task CreateFromCartAsync_WhenCartEmpty_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Cart?)null);

        var dto = new CreateOrderDto("John", "Doe", "Street 1", "Warsaw", "00-001");
        var result = await _sut.CreateFromCartAsync(userId, dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cart is empty");
    }

    [Fact]
    public async Task CreateFromCartAsync_WhenItemUnavailable_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ItemId = itemId, Quantity = 5, Item = new Item { Title = "Widget" } }
            }
        };
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);
        _itemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>())
            .Returns(new Item { Id = itemId, Quantity = 2, Price = 10m }); // stock = 2 < requested 5

        var dto = new CreateOrderDto("John", "Doe", "Street 1", "Warsaw", "00-001");
        var result = await _sut.CreateFromCartAsync(userId, dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("unavailable");
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidTransition_ReturnsFailure()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Delivered,
            Items = new List<OrderItem>()
        };
        _orderRepo.GetWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = new UpdateOrderStatusDto(OrderStatus.Pending); // Delivered → Pending not allowed
        var result = await _sut.UpdateStatusAsync(order.Id, dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task UpdateStatusAsync_ValidTransition_ReturnsSuccess()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>()
        };
        _orderRepo.GetWithDetailsAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var dto = new UpdateOrderStatusDto(OrderStatus.Confirmed);
        var result = await _sut.UpdateStatusAsync(order.Id, dto);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task CreateFromCartAsync_WhenSuccess_CommitsTransaction()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ItemId = itemId, Quantity = 1, Item = new Item { Title = "Widget" } }
            }
        };
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);
        _itemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>())
            .Returns(new Item { Id = itemId, Quantity = 5, Price = 10m, Title = "Widget" });

        var dto = new CreateOrderDto("A", "B", "C", "D", "00-001");
        var result = await _sut.CreateFromCartAsync(userId, dto);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).BeginTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateFromCartAsync_WhenConcurrencyConflict_RetriesAndSucceeds()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ItemId = itemId, Quantity = 1, Item = new Item { Title = "Widget" } }
            }
        };
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);

        var callCount = 0;
        _itemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>())
            .Returns(new Item { Id = itemId, Quantity = 5, Price = 10m, Title = "Widget" });
        _itemRepo.UpdateAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount == 1)
                    throw new ConcurrencyException("Conflict");
                return Task.CompletedTask;
            });

        var dto = new CreateOrderDto("A", "B", "C", "D", "00-001");
        var result = await _sut.CreateFromCartAsync(userId, dto);

        result.IsSuccess.Should().BeTrue();
        await _unitOfWork.Received(1).RollbackTransactionAsync(Arg.Any<CancellationToken>()); // first attempt
        await _unitOfWork.Received(1).CommitTransactionAsync(Arg.Any<CancellationToken>()); // second attempt
    }

    [Fact]
    public async Task CreateFromCartAsync_WhenAllRetriesFail_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ItemId = itemId, Quantity = 1, Item = new Item { Title = "Widget" } }
            }
        };
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);
        _itemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>())
            .Returns(new Item { Id = itemId, Quantity = 5, Price = 10m, Title = "Widget" });
        _itemRepo.UpdateAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyException("Conflict"));

        var dto = new CreateOrderDto("A", "B", "C", "D", "00-001");
        var result = await _sut.CreateFromCartAsync(userId, dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("concurrent stock changes");
        await _unitOfWork.Received(3).RollbackTransactionAsync(Arg.Any<CancellationToken>());
    }
}
