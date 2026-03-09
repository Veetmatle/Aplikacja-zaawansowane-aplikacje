namespace ShopApp.Application.DTOs.Item;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconUrl,
    bool IsActive,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    int ItemCount
);

public record CreateCategoryDto(
    string Name,
    string? Description,
    string? IconUrl,
    Guid? ParentCategoryId
);

public record UpdateCategoryDto(
    string? Name,
    string? Description,
    string? IconUrl,
    bool? IsActive,
    Guid? ParentCategoryId
);
