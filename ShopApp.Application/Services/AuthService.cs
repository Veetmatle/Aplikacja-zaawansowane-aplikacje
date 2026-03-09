using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Auth;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShopApp.Application.Services;

/// <summary>
/// Handles user registration, login, JWT issuance, and token refresh.
/// TODO: Implement refresh token storage (e.g. store in DB or Redis).
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        if (dto.Password != dto.ConfirmPassword)
            return Result<AuthResponseDto>.Failure("Passwords do not match.");

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return Result<AuthResponseDto>.Failure("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
        };

        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return Result<AuthResponseDto>.Failure(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, "User");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Result<AuthResponseDto>.Failure("Invalid credentials.", 401);

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.Failure("Invalid credentials.", 401);

        // Check status
        if (user.Status == Core.Enums.UserStatus.Banned)
            return Result<AuthResponseDto>.Forbidden($"Account banned: {user.BanReason}");

        if (user.Status == Core.Enums.UserStatus.TimedOut && user.TimeoutUntil > DateTime.UtcNow)
            return Result<AuthResponseDto>.Forbidden($"Account timed out until {user.TimeoutUntil:u}");

        return await GenerateAuthResponseAsync(user);
    }

    public Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        // TODO: Implement refresh token validation from DB/cache
        throw new NotImplementedException("Refresh token logic to be implemented.");
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    public Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        // TODO: Invalidate refresh token in DB/cache
        return Task.FromResult(r.Success());
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<Result<AuthResponseDto>> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var expires = DateTime.UtcNow.AddHours(double.Parse(jwtSection["ExpiryHours"] ?? "8"));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Guid.NewGuid().ToString("N"); // TODO: Store in DB

        var response = new AuthResponseDto(
            AccessToken: tokenString,
            RefreshToken: refreshToken,
            ExpiresAt: expires,
            User: new UserInfoDto(user.Id, user.FirstName, user.LastName, user.Email!, user.AvatarUrl, roles));

        return Result<AuthResponseDto>.Success(response);
    }
}
