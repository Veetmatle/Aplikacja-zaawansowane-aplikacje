using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

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

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => FromResult(await _orderService.GetMyOrdersAsync(_currentUser.UserId!.Value, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _orderService.GetByIdAsync(id, _currentUser.UserId!.Value, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto, CancellationToken ct)
        => FromResult(await _orderService.CreateFromCartAsync(_currentUser.UserId!.Value, dto, ct));

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto, CancellationToken ct)
        => FromResult(await _orderService.UpdateStatusAsync(id, dto, ct));
}
