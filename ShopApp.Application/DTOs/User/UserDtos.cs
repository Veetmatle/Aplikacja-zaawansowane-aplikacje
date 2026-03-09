using ShopApp.Core.Enums;

namespace ShopApp.Application.DTOs.User;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    UserStatus Status,
    DateTime? TimeoutUntil,
    string? BanReason,
    DateTime CreatedAt,
    IEnumerable<string> Roles
);

public record UpdateUserDto(
    string? FirstName,
    string? LastName,
    string? AvatarUrl
);

// Admin-only DTOs
public record AdminUpdateUserDto(
    string? FirstName,
    string? LastName,
    UserStatus? Status,
    string? BanReason,
    DateTime? TimeoutUntil
);

public record SetTimeoutDto(
    DateTime TimeoutUntil,
    string? Reason
);

public record BanUserDto(
    string Reason
);

public record AssignRoleDto(
    string RoleName
);
