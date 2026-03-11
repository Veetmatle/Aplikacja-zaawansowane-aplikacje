using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Exceptions;
using ShopApp.Core.Interfaces;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.Application.Services;

/// <summary>
/// Order creation from cart, status updates, history.
/// Uses IUnitOfWork for transactional multi-record operations.
/// Handles optimistic concurrency on Item.Quantity with retry.
/// </summary>
public class OrderService : IOrderService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> CreateFromCartAsync(Guid userId, CreateOrderDto dto, CancellationToken ct = default)
    {
        for (int attempt = 0; attempt < MaxConcurrencyRetries; attempt++)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId, ct);
            if (cart is null || !cart.Items.Any())
                return Result<OrderDto>.Failure("Cart is empty.");

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
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
                        return Result<OrderDto>.Failure($"Item '{cartItem.Item?.Title}' is unavailable or insufficient stock.");

                    order.Items.Add(new OrderItem
                    {
                        ItemId = cartItem.ItemId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = item.Price,
                        ItemTitleSnapshot = item.Title,
                    });

                    item.Quantity -= cartItem.Quantity;
                    await _itemRepository.UpdateAsync(item, ct); // may throw ConcurrencyException
                }

                order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);
                await _orderRepository.AddAsync(order, ct);

                // Clear cart
                foreach (var cartItem in cart.Items.ToList())
                    await _cartRepository.RemoveCartItemAsync(cartItem, ct);
                cart.Items.Clear();

                await _unitOfWork.CommitTransactionAsync(ct);
                return Result<OrderDto>.Success(MapToDto(order));
            }
            catch (ConcurrencyException) when (attempt < MaxConcurrencyRetries - 1)
            {
                // Concurrency conflict — another buyer modified item stock.
                // Rollback and retry with fresh data.
                await _unitOfWork.RollbackTransactionAsync(ct);
                continue;
            }
            catch (ConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return Result<OrderDto>.Failure(
                    "Unable to complete order due to concurrent stock changes. Please try again.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        return Result<OrderDto>.Failure("Unable to complete order. Please try again.");
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

        // Validate status transitions
        var allowed = GetAllowedTransitions(order.Status);
        if (!allowed.Contains(dto.Status))
            return Result<OrderDto>.Failure($"Cannot transition from {order.Status} to {dto.Status}.");

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepository.UpdateAsync(order, ct);
        return Result<OrderDto>.Success(MapToDto(order));
    }

    private static HashSet<OrderStatus> GetAllowedTransitions(OrderStatus current) => current switch
    {
        OrderStatus.Pending => new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
        OrderStatus.Confirmed => new() { OrderStatus.Shipped, OrderStatus.Cancelled },
        OrderStatus.Shipped => new() { OrderStatus.Delivered },
        OrderStatus.Delivered => new() { OrderStatus.Refunded },
        OrderStatus.Cancelled => new(),
        OrderStatus.Refunded => new(),
        _ => new()
    };

    private static string GenerateOrderNumber() =>
        $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

    private static OrderDto MapToDto(Order order) => new(
        order.Id, order.OrderNumber, order.Status, order.PaymentStatus, order.TotalAmount, order.Notes,
        order.ShippingFirstName, order.ShippingLastName, order.ShippingAddress,
        order.ShippingCity, order.ShippingPostalCode, order.ShippingCountry, order.CreatedAt,
        order.Items.Select(i => new OrderItemDto(i.Id, i.ItemId, i.ItemTitleSnapshot, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity)));
}
