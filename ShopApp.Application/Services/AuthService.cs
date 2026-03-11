using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Auth;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Interfaces.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ShopApp.Application.Services;

/// <summary>
/// Handles user registration, login, JWT issuance, refresh token rotation, and logout.
/// Refresh tokens are stored as SHA-256 hashes in the database.
/// 
/// Note: UserManager/SignInManager APIs do not accept CancellationToken —
/// this is an ASP.NET Core Identity limitation. CancellationToken is propagated
/// to all IRefreshTokenRepository calls which use EF Core directly.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _refreshTokenRepository = refreshTokenRepository;
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

        return await GenerateAuthResponseAsync(user, ct);
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

        return await GenerateAuthResponseAsync(user, ct);
    }

    public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var tokenHash = HashToken(dto.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken is null)
            return Result<AuthResponseDto>.Failure("Invalid refresh token.", 401);

        if (storedToken.IsRevoked)
        {
            // Token reuse detected — revoke all tokens for this user (security)
            await _refreshTokenRepository.RevokeAllByUserIdAsync(
                storedToken.UserId, "Token reuse detected", ct);
            return Result<AuthResponseDto>.Failure("Token has been revoked. All sessions terminated for security.", 401);
        }

        if (storedToken.IsExpired)
            return Result<AuthResponseDto>.Failure("Refresh token has expired.", 401);

        // Revoke current token (rotation)
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokeReason = "Rotated";
        await _refreshTokenRepository.UpdateAsync(storedToken, ct);

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null)
            return Result<AuthResponseDto>.Failure("User not found.", 401);

        if (user.Status == Core.Enums.UserStatus.Banned)
            return Result<AuthResponseDto>.Forbidden($"Account banned: {user.BanReason}");

        var response = await GenerateAuthResponseAsync(user, ct);

        // Link old token to new one
        if (response.IsSuccess)
        {
            var newTokenHash = HashToken(response.Value!.RefreshToken);
            storedToken.ReplacedByTokenHash = newTokenHash;
            await _refreshTokenRepository.UpdateAsync(storedToken, ct);
        }

        return response;
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result.NotFound();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        // Revoke all refresh tokens on password change
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, "Password changed", ct);

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, "User logged out", ct);
        return Result.Success();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task<Result<AuthResponseDto>> GenerateAuthResponseAsync(ApplicationUser user, CancellationToken ct = default)
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

        // Generate refresh token and persist hash in DB
        var refreshTokenPlain = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshTokenPlain);

        var refreshTokenEntity = new RefreshToken
        {
            TokenHash = refreshTokenHash,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        };
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, ct);

        var response = new AuthResponseDto(
            AccessToken: tokenString,
            RefreshToken: refreshTokenPlain,
            ExpiresAt: expires,
            User: new UserInfoDto(user.Id, user.FirstName, user.LastName, user.Email!, user.AvatarUrl, roles));

        return Result<AuthResponseDto>.Success(response);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
