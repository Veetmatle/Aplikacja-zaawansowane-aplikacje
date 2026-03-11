using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.Interfaces;

namespace ShopApp.API.Controllers;

/// <summary>Category management — public read, admin CRUD.</summary>
[Route("api/categories")]
public class CategoriesController : BaseController
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>Get all categories.</summary>
    /// <response code="200">List of categories</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => FromResult(await _categoryService.GetAllAsync(ct));

    /// <summary>Get category by ID.</summary>
    /// <response code="200">Category details</response>
    /// <response code="404">Category not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _categoryService.GetByIdAsync(id, ct));

    /// <summary>Create a new category (Admin only).</summary>
    /// <response code="200">Created category</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto, CancellationToken ct)
        => FromResult(await _categoryService.CreateAsync(dto, ct));

    /// <summary>Update a category (Admin only).</summary>
    /// <response code="200">Updated category</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">Category not found</response>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
        => FromResult(await _categoryService.UpdateAsync(id, dto, ct));

    /// <summary>Delete a category (Admin only, soft-delete).</summary>
    /// <response code="204">Category deleted</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">Category not found</response>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => FromResult(await _categoryService.DeleteAsync(id, ct));
}
