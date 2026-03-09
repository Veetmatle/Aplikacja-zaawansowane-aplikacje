using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.DTOs.Cart;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>Cart works for both guests (X-Session-Id header) and logged-in users.</summary>
[Route("api/cart")]
public class CartController : BaseController
{
    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUser;

    public CartController(ICartService cartService, ICurrentUserService currentUser)
    {
        _cartService = cartService;
        _currentUser = currentUser;
    }

    private string? SessionId => Request.Headers["X-Session-Id"].FirstOrDefault();

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
        => FromResult(await _cartService.GetCartAsync(_currentUser.UserId, SessionId, ct));

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto, CancellationToken ct)
        => FromResult(await _cartService.AddItemAsync(_currentUser.UserId, SessionId, dto, ct));

    [HttpPut("items/{cartItemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
        => FromResult(await _cartService.UpdateItemAsync(_currentUser.UserId, SessionId, cartItemId, dto, ct));

    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid cartItemId, CancellationToken ct)
        => FromResult(await _cartService.RemoveItemAsync(_currentUser.UserId, SessionId, cartItemId, ct));

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
        => FromResult(await _cartService.ClearCartAsync(_currentUser.UserId, SessionId, ct));
}
