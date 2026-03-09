using System.ComponentModel.DataAnnotations;

namespace ShopApp.Application.DTOs.Auth;

public record RegisterDto(
    [Required] string FirstName,
    [Required] string LastName,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string ConfirmPassword
);

public record LoginDto(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User
);

public record UserInfoDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    IEnumerable<string> Roles
);

public record RefreshTokenDto(
    [Required] string RefreshToken
);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword
);
