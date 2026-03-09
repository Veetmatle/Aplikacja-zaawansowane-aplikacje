using ShopApp.Core.Enums;

namespace ShopApp.Application.DTOs.Order;

public record OrderDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal TotalAmount,
    string? Notes,
    string ShippingFirstName,
    string ShippingLastName,
    string ShippingAddress,
    string ShippingCity,
    string ShippingPostalCode,
    string ShippingCountry,
    DateTime CreatedAt,
    IEnumerable<OrderItemDto> Items
);

public record OrderItemDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal
);

public record CreateOrderDto(
    string FirstName,
    string LastName,
    string Address,
    string City,
    string PostalCode,
    string Country = "PL",
    string? Notes = null
);

public record UpdateOrderStatusDto(
    OrderStatus Status
);
