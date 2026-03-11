using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.Application.Services;

/// <summary>
/// Item CRUD - only authenticated users can create/edit/delete.
/// Guests and authenticated users can read.
/// ViewCount is tracked asynchronously via IViewCountTracker to avoid
/// synchronous DB writes on every GET request.
/// </summary>
public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IViewCountTracker _viewCountTracker;
    private readonly IFileStorageService _fileStorage;

    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private const int MaxPhotosPerItem = 5;

    public ItemService(
        IItemRepository itemRepository,
        ICategoryRepository categoryRepository,
        IViewCountTracker viewCountTracker,
        IFileStorageService fileStorage)
    {
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _viewCountTracker = viewCountTracker;
        _fileStorage = fileStorage;
    }

    public async Task<Result<PagedResult<ItemSummaryDto>>> GetItemsAsync(ItemQueryDto query, CancellationToken ct = default)
    {
        var (items, total) = await _itemRepository.GetPagedAsync(
            query.Page, query.PageSize, query.CategoryId, query.Search, query.MinPrice, query.MaxPrice, ct);

        var dtos = items.Select(i => new ItemSummaryDto(
            i.Id, i.Title, i.Price, i.Status, i.Condition, i.Location, i.CreatedAt,
            i.Category.Name, $"{i.Seller.FirstName} {i.Seller.LastName}",
            i.Photos.FirstOrDefault(p => p.IsPrimary)?.Url));

        return Result<PagedResult<ItemSummaryDto>>.Success(new PagedResult<ItemSummaryDto>
        {
            Items = dtos, TotalCount = total, Page = query.Page, PageSize = query.PageSize
        });
    }

    public async Task<Result<ItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _itemRepository.GetWithDetailsAsync(id, ct);
        if (item is null) return Result<ItemDto>.NotFound();
        // Fire-and-forget view count increment — no synchronous DB write on reads
        _viewCountTracker.Track(item.Id);
        return Result<ItemDto>.Success(MapToDto(item));
    }

    public async Task<Result<ItemDto>> CreateAsync(Guid sellerId, CreateItemDto dto, CancellationToken ct = default)
    {
        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, ct);
        if (category is null) return Result<ItemDto>.Failure("Category not found.");

        var item = new Core.Entities.Item
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            Quantity = dto.Quantity,
            Condition = dto.Condition,
            Location = dto.Location,
            CategoryId = dto.CategoryId,
            SellerId = sellerId,
            ExpiresAt = dto.ExpiresAt,
        };

        await _itemRepository.AddAsync(item, ct);
        // Reload with navigation properties
        var created = await _itemRepository.GetWithDetailsAsync(item.Id, ct);
        return Result<ItemDto>.Success(MapToDto(created!));
    }

    public async Task<Result<ItemDto>> UpdateAsync(Guid itemId, Guid requestingUserId, UpdateItemDto dto, CancellationToken ct = default)
    {
        var item = await _itemRepository.GetWithDetailsAsync(itemId, ct);
        if (item is null) return Result<ItemDto>.NotFound();
        if (item.SellerId != requestingUserId) return Result<ItemDto>.Forbidden();

        if (dto.Title is not null) item.Title = dto.Title;
        if (dto.Description is not null) item.Description = dto.Description;
        if (dto.Price.HasValue) item.Price = dto.Price.Value;
        if (dto.Quantity.HasValue) item.Quantity = dto.Quantity.Value;
        if (dto.Condition.HasValue) item.Condition = dto.Condition.Value;
        if (dto.Location is not null) item.Location = dto.Location;
        if (dto.CategoryId.HasValue) item.CategoryId = dto.CategoryId.Value;
        if (dto.Status.HasValue) item.Status = dto.Status.Value;
        if (dto.ExpiresAt.HasValue) item.ExpiresAt = dto.ExpiresAt.Value;
        item.UpdatedAt = DateTime.UtcNow;

        await _itemRepository.UpdateAsync(item, ct);
        return Result<ItemDto>.Success(MapToDto(item));
    }

    public async Task<Result> DeleteAsync(Guid itemId, Guid requestingUserId, CancellationToken ct = default)
    {
        var item = await _itemRepository.GetByIdAsync(itemId, ct);
        if (item is null) return Result.NotFound();
        if (item.SellerId != requestingUserId) return Result.Failure("Forbidden.", 403);

        await _itemRepository.DeleteAsync(item, ct);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<ItemSummaryDto>>> GetMyItemsAsync(Guid sellerId, CancellationToken ct = default)
    {
        var items = await _itemRepository.GetBySellerIdAsync(sellerId, ct);
        var dtos = items.Select(i => new ItemSummaryDto(
            i.Id, i.Title, i.Price, i.Status, i.Condition, i.Location, i.CreatedAt,
            i.Category?.Name ?? "", $"{i.Seller?.FirstName} {i.Seller?.LastName}",
            i.Photos.FirstOrDefault(p => p.IsPrimary)?.Url));
        return Result<IEnumerable<ItemSummaryDto>>.Success(dtos);
    }

    public async Task<Result<IEnumerable<ItemPhotoDto>>> UploadPhotosAsync(
        Guid itemId, Guid requestingUserId,
        IEnumerable<(Stream Stream, string FileName, string ContentType, long Size)> files,
        CancellationToken ct = default)
    {
        var item = await _itemRepository.GetWithDetailsAsync(itemId, ct);
        if (item is null) return Result<IEnumerable<ItemPhotoDto>>.NotFound();
        if (item.SellerId != requestingUserId) return Result<IEnumerable<ItemPhotoDto>>.Forbidden();

        var fileList = files.ToList();
        if (item.Photos.Count + fileList.Count > MaxPhotosPerItem)
            return Result<IEnumerable<ItemPhotoDto>>.Failure(
                $"Maximum {MaxPhotosPerItem} photos per item. Currently {item.Photos.Count}, trying to add {fileList.Count}.");

        foreach (var f in fileList)
        {
            if (!AllowedTypes.Contains(f.ContentType))
                return Result<IEnumerable<ItemPhotoDto>>.Failure($"File type '{f.ContentType}' not allowed. Use JPEG, PNG or WebP.");
            if (f.Size > MaxFileSize)
                return Result<IEnumerable<ItemPhotoDto>>.Failure($"File '{f.FileName}' exceeds 5 MB limit.");
        }

        var hasPrimary = item.Photos.Any(p => p.IsPrimary);
        var order = item.Photos.Count;

        foreach (var f in fileList)
        {
            var url = await _fileStorage.UploadAsync(f.Stream, f.FileName, f.ContentType, ct);
            var photo = new Core.Entities.ItemPhoto
            {
                Url = url,
                AltText = item.Title,
                IsPrimary = !hasPrimary,
                Order = order++,
                ItemId = itemId,
            };
            item.Photos.Add(photo);
            hasPrimary = true;
        }

        await _itemRepository.UpdateAsync(item, ct);

        return Result<IEnumerable<ItemPhotoDto>>.Success(
            item.Photos.Select(p => new ItemPhotoDto(p.Id, p.Url, p.AltText, p.IsPrimary, p.Order)));
    }

    private static ItemDto MapToDto(Core.Entities.Item item) => new(
        item.Id, item.Title, item.Description, item.Price, item.Quantity,
        item.Status, item.Condition, item.Location, item.ViewCount, item.CreatedAt, item.ExpiresAt,
        item.CategoryId, item.Category?.Name ?? "", item.SellerId,
        $"{item.Seller?.FirstName} {item.Seller?.LastName}",
        item.Photos.Select(p => new ItemPhotoDto(p.Id, p.Url, p.AltText, p.IsPrimary, p.Order)));
}
