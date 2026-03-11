using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.DTOs.Payment;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>
/// Payment flow:
/// 1. POST /api/payments/{orderId}/initiate — start payment, get redirect URL
/// 2. P24 calls POST /api/payments/notify — callback with payment result
/// 3. GET /api/payments/{orderId}/status — check payment status
/// 4. GET /api/payments/return — redirect after payment (browser return)
/// </summary>
[Route("api/payments")]
public class PaymentsController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUserService _currentUser;

    public PaymentsController(IPaymentService paymentService, ICurrentUserService currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    /// <summary>Initiate payment for an order — returns P24 redirect URL.</summary>
    /// <response code="200">Payment initiated — redirect URL returned</response>
    /// <response code="400">Order already paid or payment init failed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the order owner</response>
    /// <response code="404">Order not found</response>
    [Authorize]
    [HttpPost("{orderId:guid}/initiate")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Initiate(Guid orderId, CancellationToken ct)
        => FromResult(await _paymentService.InitiatePaymentAsync(orderId, _currentUser.UserId!.Value, ct));

    /// <summary>P24 notification callback — AllowAnonymous because P24 calls this without JWT.</summary>
    /// <response code="200">Notification processed</response>
    /// <response code="400">Invalid signature or payment not found</response>
    [AllowAnonymous]
    [HttpPost("notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Notify([FromForm] P24NotificationDto dto, CancellationToken ct)
    {
        var result = await _paymentService.HandleNotificationAsync(dto, ct);
        // P24 expects 200 OK regardless (to stop retrying)
        return result.IsSuccess ? Ok() : BadRequest();
    }

    /// <summary>Check payment status for an order.</summary>
    /// <response code="200">Payment status</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not the order owner</response>
    /// <response code="404">Order or payment not found</response>
    [Authorize]
    [HttpGet("{orderId:guid}/status")]
    [ProducesResponseType(typeof(PaymentStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(Guid orderId, CancellationToken ct)
        => FromResult(await _paymentService.GetPaymentStatusAsync(orderId, _currentUser.UserId!.Value, ct));

    /// <summary>Return URL — P24 redirects user here after payment.</summary>
    /// <response code="200">Payment return message</response>
    [AllowAnonymous]
    [HttpGet("return")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Return([FromQuery] Guid orderId)
    {
        // In a real app, this would redirect to a frontend page
        return Ok(new { message = "Payment processed. Check order status.", orderId });
    }
}
