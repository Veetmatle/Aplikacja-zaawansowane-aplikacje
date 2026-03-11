using Microsoft.AspNetCore.Identity;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.User;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;

namespace ShopApp.Application.Services;

/// <summary>
/// User self-service: view and update own profile.
/// Note: UserManager API does not accept CancellationToken (ASP.NET Core Identity limitation).
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null) return Result<UserDto>.NotFound();
        return Result<UserDto>.Success(await MapToDtoAsync(user));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<UserDto>.NotFound();

        if (dto.FirstName is not null) user.FirstName = dto.FirstName;
        if (dto.LastName is not null) user.LastName = dto.LastName;
        if (dto.AvatarUrl is not null) user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return Result<UserDto>.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Result<UserDto>.Success(await MapToDtoAsync(user));
    }

    public Task<Result<PagedResult<UserDto>>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        // TODO: Implement paged user list
        throw new NotImplementedException();
    }

    private async Task<UserDto> MapToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.FirstName, user.LastName, user.Email!,
            user.AvatarUrl, user.Status, user.TimeoutUntil, user.BanReason, user.CreatedAt, roles);
    }
}
