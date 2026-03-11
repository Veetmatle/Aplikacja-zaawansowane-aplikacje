using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.DTOs.User;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>Current user profile management.</summary>
[Authorize]
[Route("api/users")]
public class UsersController : BaseController
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IUserService userService, ICurrentUserService currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    /// <summary>Get current user's profile.</summary>
    /// <response code="200">User profile</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe(CancellationToken ct)
        => FromResult(await _userService.GetByIdAsync(_currentUser.UserId!.Value, ct));

    /// <summary>Update current user's profile.</summary>
    /// <response code="200">Updated profile</response>
    /// <response code="400">Validation error</response>
    /// <response code="401">Not authenticated</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto, CancellationToken ct)
        => FromResult(await _userService.UpdateProfileAsync(_currentUser.UserId!.Value, dto, ct));
}
