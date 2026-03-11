using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopApp.Application.DTOs.Auth;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>Register, login, refresh, logout, change password.</summary>
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Register a new user account.</summary>
    /// <response code="200">Registration successful — returns JWT tokens</response>
    /// <response code="400">Validation error or email already taken</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
        => FromResult(await _authService.RegisterAsync(dto, ct));

    /// <summary>Login with email and password.</summary>
    /// <response code="200">Login successful — returns JWT tokens</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        => FromResult(await _authService.LoginAsync(dto, ct));

    /// <summary>Refresh access token using refresh token.</summary>
    /// <response code="200">New token pair</response>
    /// <response code="400">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto, CancellationToken ct)
        => FromResult(await _authService.RefreshTokenAsync(dto, ct));

    /// <summary>Logout — revoke all refresh tokens for current user.</summary>
    /// <response code="204">Logout successful</response>
    /// <response code="401">Not authenticated</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken ct)
        => FromResult(await _authService.LogoutAsync(_currentUser.UserId!.Value, ct));

    /// <summary>Change password for current user.</summary>
    /// <response code="204">Password changed successfully</response>
    /// <response code="400">Invalid current password or validation error</response>
    /// <response code="401">Not authenticated</response>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
        => FromResult(await _authService.ChangePasswordAsync(_currentUser.UserId!.Value, dto, ct));
}
