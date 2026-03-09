namespace ShopApp.Application.DTOs.Cart;

public record CartDto(
    Guid Id,
    IEnumerable<CartItemDto> Items,
    decimal TotalAmount,
    int TotalItems
);

public record CartItemDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    string? ItemPhotoUrl,
    decimal UnitPrice,
    int Quantity,
    decimal SubTotal
);

public record AddToCartDto(
    Guid ItemId,
    int Quantity = 1
);

public record UpdateCartItemDto(
    int Quantity
);
