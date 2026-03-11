using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>Order management — create from cart, view, admin status updates.</summary>
[Authorize]
[Route("api/orders")]
public class OrdersController : BaseController
{
    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUser;

    public OrdersController(IOrderService orderService, ICurrentUserService currentUser)
    {
        _orderService = orderService;
        _currentUser = currentUser;
    }

    /// <summary>Get current user's orders.</summary>
    /// <response code="200">List of orders</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => FromResult(await _orderService.GetMyOrdersAsync(_currentUser.UserId!.Value, ct));

    /// <summary>Get order details by ID.</summary>
    /// <response code="200">Order details</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the order owner</response>
    /// <response code="404">Order not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _orderService.GetByIdAsync(id, _currentUser.UserId!.Value, ct));

    /// <summary>Create order from cart contents.</summary>
    /// <response code="200">Created order</response>
    /// <response code="400">Validation error or empty cart</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
        => FromResult(await _orderService.CreateFromCartAsync(_currentUser.UserId!.Value, dto, ct));

    /// <summary>Update order status (Admin only). Validates status transitions.</summary>
    /// <response code="200">Updated order</response>
    /// <response code="400">Invalid status transition</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">Order not found</response>
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct)
        => FromResult(await _orderService.UpdateStatusAsync(id, dto, ct));
}
