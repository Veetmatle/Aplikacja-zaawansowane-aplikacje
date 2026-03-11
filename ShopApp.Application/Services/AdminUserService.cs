using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.User;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;

namespace ShopApp.Application.Services;

/// <summary>
/// Admin operations: ban, timeout, role management, user deletion.
/// Note: UserManager API does not accept CancellationToken — this is an
/// ASP.NET Core Identity limitation, not a project oversight.
/// CancellationToken is propagated to direct EF Core calls (CountAsync, ToListAsync).
/// </summary>
public class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> GetUserDetailsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<UserDto>.NotFound();
        return Result<UserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<Result<PagedResult<UserDto>>> GetAllUsersAsync(int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.Email!.Contains(search) || u.FirstName.Contains(search) || u.LastName.Contains(search));

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<UserDto>();
        foreach (var u in users) dtos.Add(await MapToDtoAsync(u));

        return Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
        {
            Items = dtos, TotalCount = total, Page = page, PageSize = pageSize
        });
    }

    public async Task<Result> BanUserAsync(Guid userId, BanUserDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        user.Status = UserStatus.Banned;
        user.BanReason = dto.Reason;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> UnbanUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        user.Status = UserStatus.Active;
        user.BanReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> SetTimeoutAsync(Guid userId, SetTimeoutDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        user.Status = UserStatus.TimedOut;
        user.TimeoutUntil = dto.TimeoutUntil;
        user.BanReason = dto.Reason;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> RemoveTimeoutAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        user.Status = UserStatus.Active;
        user.TimeoutUntil = null;
        user.BanReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> AssignRoleAsync(Guid userId, AssignRoleDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        if (await _userManager.IsInRoleAsync(user, dto.RoleName))
            return Result.Failure("User already has this role.");

        var result = await _userManager.AddToRoleAsync(user, dto.RoleName);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        // UserManager.FindByIdAsync does not accept CancellationToken (Identity limitation)
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        // Soft delete — set DeletedAt instead of physically removing the row.
        // GlobalQueryFilter on ApplicationUser ensures soft-deleted users are excluded from queries.
        // UserManager.UpdateAsync persists changes through Identity's UserStore.
        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserStatus.Banned;
        user.BanReason = "Account deleted by admin.";
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Result.Success() : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private async Task<UserDto> MapToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.FirstName, user.LastName, user.Email!,
            user.AvatarUrl, user.Status, user.TimeoutUntil, user.BanReason, user.CreatedAt, roles);
    }
}
