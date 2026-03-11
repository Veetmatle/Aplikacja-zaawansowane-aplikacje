using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.User;
using ShopApp.Application.Interfaces;

namespace ShopApp.API.Controllers;

/// <summary>Admin panel: full user management.</summary>
[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
public class AdminUsersController : BaseController
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>Get all users (paginated, searchable).</summary>
    /// <response code="200">Paged list of users</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
        => FromResult(await _adminUserService.GetAllUsersAsync(page, pageSize, search, ct));

    /// <summary>Get user details by ID.</summary>
    /// <response code="200">User details</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.GetUserDetailsAsync(id, ct));

    /// <summary>Ban a user.</summary>
    /// <response code="204">User banned</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id:guid}/ban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Ban(Guid id, [FromBody] BanUserDto dto, CancellationToken ct)
        => FromResult(await _adminUserService.BanUserAsync(id, dto, ct));

    /// <summary>Unban a user.</summary>
    /// <response code="204">User unbanned</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id:guid}/unban")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unban(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.UnbanUserAsync(id, ct));

    /// <summary>Set a timeout on a user.</summary>
    /// <response code="204">Timeout set</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id:guid}/timeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Timeout(Guid id, [FromBody] SetTimeoutDto dto, CancellationToken ct)
        => FromResult(await _adminUserService.SetTimeoutAsync(id, dto, ct));

    /// <summary>Remove timeout from a user.</summary>
    /// <response code="204">Timeout removed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id:guid}/timeout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTimeout(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.RemoveTimeoutAsync(id, ct));

    /// <summary>Assign role to user.</summary>
    /// <response code="204">Role assigned</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleDto dto, CancellationToken ct)
        => FromResult(await _adminUserService.AssignRoleAsync(id, dto, ct));

    /// <summary>Remove role from user.</summary>
    /// <response code="204">Role removed</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id:guid}/roles/{roleName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRole(Guid id, string roleName, CancellationToken ct)
        => FromResult(await _adminUserService.RemoveRoleAsync(id, roleName, ct));

    /// <summary>Delete a user account.</summary>
    /// <response code="204">User deleted</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not admin</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.DeleteUserAsync(id, ct));
}
