using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ItemQueryDto query, CancellationToken ct)
        => FromResult(await _itemService.GetItemsAsync(query, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _itemService.GetByIdAsync(id, ct));

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => FromResult(await _itemService.GetMyItemsAsync(_currentUser.UserId!.Value, ct));

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateItemDto dto, CancellationToken ct)
        => FromResult(await _itemService.CreateAsync(_currentUser.UserId!.Value, dto, ct));

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemDto dto, CancellationToken ct)
        => FromResult(await _itemService.UpdateAsync(id, _currentUser.UserId!.Value, dto, ct));

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => FromResult(await _itemService.DeleteAsync(id, _currentUser.UserId!.Value, ct));
}
