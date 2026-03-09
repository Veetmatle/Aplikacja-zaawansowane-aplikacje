using ShopApp.Core.Enums;

namespace ShopApp.Application.DTOs.Item;

public record ItemDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    int Quantity,
    ItemStatus Status,
    ItemCondition Condition,
    string? Location,
    int ViewCount,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    Guid CategoryId,
    string CategoryName,
    Guid SellerId,
    string SellerName,
    IEnumerable<ItemPhotoDto> Photos
);

public record ItemPhotoDto(
    Guid Id,
    string Url,
    string? AltText,
    bool IsPrimary,
    int Order
);

public record ItemSummaryDto(
    Guid Id,
    string Title,
    decimal Price,
    ItemStatus Status,
    ItemCondition Condition,
    string? Location,
    DateTime CreatedAt,
    string CategoryName,
    string SellerName,
    string? PrimaryPhotoUrl
);

public record CreateItemDto(
    string Title,
    string Description,
    decimal Price,
    int Quantity,
    ItemCondition Condition,
    string? Location,
    Guid CategoryId,
    DateTime? ExpiresAt
);

public record UpdateItemDto(
    string? Title,
    string? Description,
    decimal? Price,
    int? Quantity,
    ItemCondition? Condition,
    string? Location,
    Guid? CategoryId,
    ItemStatus? Status,
    DateTime? ExpiresAt
);

public record ItemQueryDto(
    int Page = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
);
