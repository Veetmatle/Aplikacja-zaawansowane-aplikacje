using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken ct = default)
        => FromResult(await _adminUserService.GetAllUsersAsync(page, pageSize, search, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.GetUserDetailsAsync(id, ct));

    [HttpPost("{id:guid}/ban")]
    public async Task<IActionResult> Ban(Guid id, [FromBody] BanUserDto dto, CancellationToken ct)
        => FromResult(await _adminUserService.BanUserAsync(id, dto, ct));

    [HttpPost("{id:guid}/unban")]
    public async Task<IActionResult> Unban(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.UnbanUserAsync(id, ct));

    [HttpPost("{id:guid}/timeout")]
    public async Task<IActionResult> Timeout(Guid id, [FromBody] SetTimeoutDto dto, CancellationToken ct)
        => FromResult(await _adminUserService.SetTimeoutAsync(id, dto, ct));

    [HttpDelete("{id:guid}/timeout")]
    public async Task<IActionResult> RemoveTimeout(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.RemoveTimeoutAsync(id, ct));

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleDto dto, CancellationToken ct)
        => FromResult(await _adminUserService.AssignRoleAsync(id, dto, ct));

    [HttpDelete("{id:guid}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(Guid id, string roleName, CancellationToken ct)
        => FromResult(await _adminUserService.RemoveRoleAsync(id, roleName, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        => FromResult(await _adminUserService.DeleteUserAsync(id, ct));
}
