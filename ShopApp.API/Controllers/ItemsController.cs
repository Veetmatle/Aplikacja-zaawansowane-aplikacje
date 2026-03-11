using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>Public browsing + authenticated create/edit/delete.</summary>
[Route("api/items")]
public class ItemsController : BaseController
{
    private readonly IItemService _itemService;
    private readonly ICurrentUserService _currentUser;

    public ItemsController(IItemService itemService, ICurrentUserService currentUser)
    {
        _itemService = itemService;
        _currentUser = currentUser;
    }

    /// <summary>Get paginated items with optional filtering.</summary>
    /// <response code="200">Paged list of items</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ItemSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] ItemQueryDto query, CancellationToken ct)
        => FromResult(await _itemService.GetItemsAsync(query, ct));

    /// <summary>Get item details by ID (increments view count).</summary>
    /// <response code="200">Item details</response>
    /// <response code="404">Item not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _itemService.GetByIdAsync(id, ct));

    /// <summary>Get current user's items.</summary>
    /// <response code="200">List of user's items</response>
    /// <response code="401">Not authenticated</response>
    [Authorize]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IEnumerable<ItemSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => FromResult(await _itemService.GetMyItemsAsync(_currentUser.UserId!.Value, ct));

    /// <summary>Create a new item listing.</summary>
    /// <response code="200">Created item</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateItemDto dto, CancellationToken ct)
        => FromResult(await _itemService.CreateAsync(_currentUser.UserId!.Value, dto, ct));

    /// <summary>Update an existing item (owner only).</summary>
    /// <response code="200">Updated item</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the item owner</response>
    /// <response code="404">Item not found</response>
    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemDto dto, CancellationToken ct)
        => FromResult(await _itemService.UpdateAsync(id, _currentUser.UserId!.Value, dto, ct));

    /// <summary>Delete an item (owner only, soft-delete).</summary>
    /// <response code="204">Item deleted</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the item owner</response>
    /// <response code="404">Item not found</response>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => FromResult(await _itemService.DeleteAsync(id, _currentUser.UserId!.Value, ct));
}
