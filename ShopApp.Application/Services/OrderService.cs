using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.Application.Services;

/// <summary>
/// Order creation from cart, status updates, history.
/// TODO: Integrate payment gateway.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IItemRepository _itemRepository;

    public OrderService(IOrderRepository orderRepository, ICartRepository cartRepository, IItemRepository itemRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _itemRepository = itemRepository;
    }

    public async Task<Result<OrderDto>> CreateFromCartAsync(Guid userId, CreateOrderDto dto, CancellationToken ct = default)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId, ct);
        if (cart is null || !cart.Items.Any())
            return Result<OrderDto>.Failure("Cart is empty.");

        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(),
            BuyerId = userId,
            ShippingFirstName = dto.FirstName,
            ShippingLastName = dto.LastName,
            ShippingAddress = dto.Address,
            ShippingCity = dto.City,
            ShippingPostalCode = dto.PostalCode,
            ShippingCountry = dto.Country,
            Notes = dto.Notes,
        };

        foreach (var cartItem in cart.Items)
        {
            var item = await _itemRepository.GetByIdAsync(cartItem.ItemId, ct);
            if (item is null || item.Quantity < cartItem.Quantity)
                return Result<OrderDto>.Failure($"Item '{cartItem.Item?.Title}' is unavailable.");

            order.Items.Add(new OrderItem
            {
                ItemId = cartItem.ItemId,
                Quantity = cartItem.Quantity,
                UnitPrice = item.Price,
                ItemTitleSnapshot = item.Title,
            });

            item.Quantity -= cartItem.Quantity;
            await _itemRepository.UpdateAsync(item, ct);
        }

        order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
        await _orderRepository.AddAsync(order, ct);
        cart.Items.Clear();
        await _cartRepository.UpdateAsync(cart, ct);

        return Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<Result<OrderDto>> GetByIdAsync(Guid orderId, Guid requestingUserId, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetWithDetailsAsync(orderId, ct);
        if (order is null) return Result<OrderDto>.NotFound();
        if (order.BuyerId != requestingUserId) return Result<OrderDto>.Forbidden();
        return Result<OrderDto>.Success(MapToDto(order));
    }

    public async Task<Result<IEnumerable<OrderDto>>> GetMyOrdersAsync(Guid userId, CancellationToken ct = default)
    {
        var orders = await _orderRepository.GetByBuyerIdAsync(userId, ct);
        return Result<IEnumerable<OrderDto>>.Success(orders.Select(MapToDto));
    }

    public async Task<Result<OrderDto>> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, CancellationToken ct = default)
    {
        var order = await _orderRepository.GetWithDetailsAsync(orderId, ct);
        if (order is null) return Result<OrderDto>.NotFound();
        order.Status = dto.Status;
        await _orderRepository.UpdateAsync(order, ct);
        return Result<OrderDto>.Success(MapToDto(order));
    }

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

    private static OrderDto MapToDto(Order order) => new(
        order.Id, order.OrderNumber, order.Status, order.TotalAmount, order.Notes,
        order.ShippingFirstName, order.ShippingLastName, order.ShippingAddress,
        order.ShippingCity, order.ShippingPostalCode, order.ShippingCountry, order.CreatedAt,
        order.Items.Select(i => new OrderItemDto(i.Id, i.ItemId, i.ItemTitleSnapshot, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity)));
}
