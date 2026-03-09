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

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
        => FromResult(await _userService.GetByIdAsync(_currentUser.UserId!.Value, ct));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto, CancellationToken ct)
        => FromResult(await _userService.UpdateProfileAsync(_currentUser.UserId!.Value, dto, ct));
}
