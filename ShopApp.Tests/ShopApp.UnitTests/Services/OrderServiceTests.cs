using FluentAssertions;
using NSubstitute;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Services;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.UnitTests.Services;

public class OrderServiceTests
{
    private readonly IOrderRepository _orderRepo;
    private readonly ICartRepository _cartRepo;
    private readonly IItemRepository _itemRepo;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _orderRepo = Substitute.For<IOrderRepository>();
        _cartRepo = Substitute.For<ICartRepository>();
        _itemRepo = Substitute.For<IItemRepository>();
        _sut = new OrderService(_orderRepo, _cartRepo, _itemRepo);
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
}
