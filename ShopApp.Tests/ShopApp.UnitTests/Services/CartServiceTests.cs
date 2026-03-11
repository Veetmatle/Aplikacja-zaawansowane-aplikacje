using FluentAssertions;
using NSubstitute;
using ShopApp.Application.DTOs.Cart;
using ShopApp.Application.Services;
using ShopApp.Core.Entities;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.UnitTests.Services;

public class CartServiceTests
{
    private readonly ICartRepository _cartRepo;
    private readonly IItemRepository _itemRepo;
    private readonly CartService _sut;

    public CartServiceTests()
    {
        _cartRepo = Substitute.For<ICartRepository>();
        _itemRepo = Substitute.For<IItemRepository>();
        _sut = new CartService(_cartRepo, _itemRepo);
    }

    [Fact]
    public async Task AddItemAsync_WhenItemNotFound_ReturnsNotFound()
    {
        _itemRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Item?)null);

        var dto = new AddToCartDto(Guid.NewGuid(), 1);
        var result = await _sut.AddItemAsync(Guid.NewGuid(), null, dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddItemAsync_WhenInsufficientStock_ReturnsFailure()
    {
        var item = new Item { Id = Guid.NewGuid(), Title = "Test", Quantity = 2 };
        _itemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var userId = Guid.NewGuid();
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new Cart { UserId = userId, Items = new List<CartItem>() });

        var dto = new AddToCartDto(item.Id, 5);
        var result = await _sut.AddItemAsync(userId, null, dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task AddItemAsync_WhenValid_AddsToCartAndReturnsSuccess()
    {
        var itemId = Guid.NewGuid();
        var item = new Item { Id = itemId, Title = "Widget", Price = 25m, Quantity = 10 };
        _itemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns(item);

        var userId = Guid.NewGuid();
        var cart = new Cart { UserId = userId, Items = new List<CartItem>() };
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);

        var dto = new AddToCartDto(itemId, 2);
        var result = await _sut.AddItemAsync(userId, null, dto);

        result.IsSuccess.Should().BeTrue();
        await _cartRepo.Received(1).AddCartItemAsync(Arg.Any<CartItem>(), Arg.Any<CancellationToken>());
        cart.Items.Should().HaveCount(1);
        cart.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task ClearCartAsync_ClearsAllItems()
    {
        var userId = Guid.NewGuid();
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ItemId = Guid.NewGuid(), Quantity = 1 },
                new() { ItemId = Guid.NewGuid(), Quantity = 3 }
            }
        };
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(cart);

        var result = await _sut.ClearCartAsync(userId, null);

        result.IsSuccess.Should().BeTrue();
        cart.Items.Should().BeEmpty();
        await _cartRepo.Received(2).RemoveCartItemAsync(Arg.Any<CartItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeGuestCartAsync_MergesItemsIntoUserCart()
    {
        var userId = Guid.NewGuid();
        var sessionId = "test-session";
        var itemId = Guid.NewGuid();

        var guestCart = new Cart
        {
            SessionId = sessionId,
            Items = new List<CartItem>
            {
                new() { ItemId = itemId, Quantity = 2 }
            }
        };
        var userCart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ItemId = itemId, Quantity = 1 }
            }
        };

        _cartRepo.GetBySessionIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(guestCart);
        _cartRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(userCart);

        var result = await _sut.MergeGuestCartAsync(userId, sessionId);

        result.IsSuccess.Should().BeTrue();
        userCart.Items.First().Quantity.Should().Be(3); // 1 + 2
        await _cartRepo.Received(1).DeleteAsync(guestCart, Arg.Any<CancellationToken>());
    }
}
