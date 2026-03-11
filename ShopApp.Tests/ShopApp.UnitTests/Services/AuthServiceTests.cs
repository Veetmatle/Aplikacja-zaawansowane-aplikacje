using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using ShopApp.Application.DTOs.Auth;
using ShopApp.Application.Services;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.UnitTests.Services;

public class AuthServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // Mock UserManager
        var userStore = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            userStore, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock SignInManager
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManager = Substitute.For<SignInManager<ApplicationUser>>(
            _userManager, contextAccessor, claimsFactory, null!, null!, null!, null!);

        _refreshTokenRepo = Substitute.For<IRefreshTokenRepository>();

        // In-memory configuration
        var configData = new Dictionary<string, string?>
        {
            { "Jwt:Key", "ThisIsATestSecretKeyThatIsLongEnough123!" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpiryHours", "1" }
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        _sut = new AuthService(_userManager, _signInManager, _configuration, _refreshTokenRepo);
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordsDoNotMatch_ReturnsFailure()
    {
        var dto = new RegisterDto("John", "Doe", "john@test.com", "Password1!", "DifferentPassword!");

        var result = await _sut.RegisterAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Passwords do not match");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsFailure()
    {
        var dto = new RegisterDto("John", "Doe", "john@test.com", "Password1!", "Password1!");
        _userManager.FindByEmailAsync(dto.Email)
            .Returns(new ApplicationUser { Email = dto.Email });

        var result = await _sut.RegisterAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already registered");
    }

    [Fact]
    public async Task RegisterAsync_WhenValid_ReturnsSuccessWithTokens()
    {
        var dto = new RegisterDto("John", "Doe", "john@test.com", "Password1!", "Password1!");
        _userManager.FindByEmailAsync(dto.Email).Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), dto.Password)
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);
        _userManager.GetRolesAsync(Arg.Any<ApplicationUser>())
            .Returns(new List<string> { "User" });

        var result = await _sut.RegisterAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AccessToken.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBeNullOrEmpty();
        await _refreshTokenRepo.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WhenUserBanned_ReturnsForbidden()
    {
        var dto = new LoginDto("banned@test.com", "Password1!");
        var user = new ApplicationUser
        {
            Email = dto.Email,
            Status = UserStatus.Banned,
            BanReason = "Spam"
        };
        _userManager.FindByEmailAsync(dto.Email).Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, dto.Password, true)
            .Returns(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var result = await _sut.LoginAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("banned");
    }

    [Fact]
    public async Task LoginAsync_WhenInvalidCredentials_Returns401()
    {
        var dto = new LoginDto("john@test.com", "WrongPassword");
        _userManager.FindByEmailAsync(dto.Email).Returns((ApplicationUser?)null);

        var result = await _sut.LoginAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenNotFound_ReturnsFailure()
    {
        var dto = new RefreshTokenDto("invalid-token");
        _refreshTokenRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var result = await _sut.RefreshTokenAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenRevoked_RevokesAllAndReturnsFailure()
    {
        var dto = new RefreshTokenDto("revoked-token");
        var revokedToken = new RefreshToken
        {
            TokenHash = "hash",
            UserId = Guid.NewGuid(),
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        _refreshTokenRepo.GetByTokenHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(revokedToken);

        var result = await _sut.RefreshTokenAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("revoked");
        await _refreshTokenRepo.Received(1).RevokeAllByUserIdAsync(
            revokedToken.UserId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutAsync_RevokesAllTokens()
    {
        var userId = Guid.NewGuid();

        var result = await _sut.LogoutAsync(userId);

        result.IsSuccess.Should().BeTrue();
        await _refreshTokenRepo.Received(1).RevokeAllByUserIdAsync(
            userId, "User logged out", Arg.Any<CancellationToken>());
    }
}
