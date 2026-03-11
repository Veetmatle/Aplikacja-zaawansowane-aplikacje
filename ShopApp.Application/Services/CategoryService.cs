using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryService(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IEnumerable<CategoryDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var cats = await _categoryRepository.GetActiveAsync(ct);
        return Result<IEnumerable<CategoryDto>>.Success(cats.Select(MapToDto));
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var cat = await _categoryRepository.GetByIdAsync(id, ct);
        if (cat is null) return Result<CategoryDto>.NotFound();
        return Result<CategoryDto>.Success(MapToDto(cat));
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        var cat = new Category
        {
            Name = dto.Name,
            Slug = dto.Name.ToLower().Replace(" ", "-"),
            Description = dto.Description,
            IconUrl = dto.IconUrl,
            ParentCategoryId = dto.ParentCategoryId,
        };
        await _categoryRepository.AddAsync(cat, ct);
        return Result<CategoryDto>.Success(MapToDto(cat));
    }

    public async Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        var cat = await _categoryRepository.GetByIdAsync(id, ct);
        if (cat is null) return Result<CategoryDto>.NotFound();

        if (dto.Name is not null) { cat.Name = dto.Name; cat.Slug = dto.Name.ToLower().Replace(" ", "-"); }
        if (dto.Description is not null) cat.Description = dto.Description;
        if (dto.IconUrl is not null) cat.IconUrl = dto.IconUrl;
        if (dto.IsActive.HasValue) cat.IsActive = dto.IsActive.Value;
        if (dto.ParentCategoryId.HasValue) cat.ParentCategoryId = dto.ParentCategoryId;
        cat.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(cat, ct);
        return Result<CategoryDto>.Success(MapToDto(cat));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var cat = await _categoryRepository.GetByIdAsync(id, ct);
        if (cat is null) return Result.NotFound();
        await _categoryRepository.DeleteAsync(cat, ct);
        return Result.Success();
    }

    private static CategoryDto MapToDto(Category c) => new(
        c.Id, c.Name, c.Slug, c.Description, c.IconUrl, c.IsActive,
        c.ParentCategoryId, c.ParentCategory?.Name, c.Items.Count);
}
