using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ShopApp.Core.Entities;
using ShopApp.Core.Interfaces;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.Core.Interfaces.Services;
using ShopApp.Infrastructure.Data;
using ShopApp.Infrastructure.Repositories;
using ShopApp.Infrastructure.Services;
using System.Text;

namespace ShopApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ──────────────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("ShopApp.Infrastructure")));

        // ── Identity ──────────────────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ── JWT ───────────────────────────────────────────────────────────────
        var jwtSection = configuration.GetSection("Jwt");
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
                ClockSkew = TimeSpan.Zero
            };
        });

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Infrastructure services ───────────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IPaymentGateway, Przelewy24Service>();

        // ── HTTP clients ──────────────────────────────────────────────────────
        services.AddHttpClient("Gemini");
        services.AddHttpClient("Przelewy24", client =>
        {
            var sandbox = configuration.GetValue("Przelewy24:Sandbox", true);
            client.BaseAddress = new Uri(sandbox
                ? "https://sandbox.przelewy24.pl/"
                : "https://secure.przelewy24.pl/");
        });

        return services;
    }
}
