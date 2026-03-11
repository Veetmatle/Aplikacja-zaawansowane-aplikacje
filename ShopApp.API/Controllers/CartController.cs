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

    /// <summary>Generate a server-side session ID for guest cart. Returns UUID in response body.</summary>
    /// <response code="200">Session ID generated</response>
    [HttpPost("session")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult CreateSession()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        return Ok(new { sessionId });
    }

    /// <summary>Get current cart contents.</summary>
    /// <response code="200">Cart contents</response>
    /// <response code="400">Missing session ID for guest</response>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated && string.IsNullOrWhiteSpace(SessionId))
            return BadRequest(new { error = "Guest cart requires X-Session-Id header. Call POST /api/cart/session first." });

        return FromResult(await _cartService.GetCartAsync(_currentUser.UserId, SessionId, ct));
    }

    /// <summary>Add an item to cart.</summary>
    /// <response code="200">Updated cart</response>
    /// <response code="400">Validation error or missing session</response>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated && string.IsNullOrWhiteSpace(SessionId))
            return BadRequest(new { error = "Guest cart requires X-Session-Id header. Call POST /api/cart/session first." });

        return FromResult(await _cartService.AddItemAsync(_currentUser.UserId, SessionId, dto, ct));
    }

    /// <summary>Update cart item quantity.</summary>
    /// <response code="200">Updated cart</response>
    /// <response code="400">Missing session or invalid quantity</response>
    /// <response code="404">Cart item not found</response>
    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated && string.IsNullOrWhiteSpace(SessionId))
            return BadRequest(new { error = "Guest cart requires X-Session-Id header. Call POST /api/cart/session first." });

        return FromResult(await _cartService.UpdateItemAsync(_currentUser.UserId, SessionId, cartItemId, dto, ct));
    }

    /// <summary>Remove an item from cart.</summary>
    /// <response code="204">Item removed</response>
    /// <response code="400">Missing session</response>
    /// <response code="404">Cart item not found</response>
    [HttpDelete("items/{cartItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(Guid cartItemId, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated && string.IsNullOrWhiteSpace(SessionId))
            return BadRequest(new { error = "Guest cart requires X-Session-Id header. Call POST /api/cart/session first." });

        return FromResult(await _cartService.RemoveItemAsync(_currentUser.UserId, SessionId, cartItemId, ct));
    }

    /// <summary>Clear all items from cart.</summary>
    /// <response code="204">Cart cleared</response>
    /// <response code="400">Missing session</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClearCart(CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated && string.IsNullOrWhiteSpace(SessionId))
            return BadRequest(new { error = "Guest cart requires X-Session-Id header. Call POST /api/cart/session first." });

        return FromResult(await _cartService.ClearCartAsync(_currentUser.UserId, SessionId, ct));
    }
}
