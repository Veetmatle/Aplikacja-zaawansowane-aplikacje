using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopApp.Infrastructure.Data;

namespace ShopApp.Infrastructure.Services;

/// <summary>
/// Periodic background service that removes expired guest carts from the database.
/// Runs every hour. Carts without ExpiresAt are skipped (user carts).
/// Uses raw SQL for efficient bulk deletion without loading entities.
/// </summary>
public sealed class ExpiredCartCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredCartCleanupService> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);

    public ExpiredCartCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpiredCartCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired cart cleanup service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await CleanupExpiredCartsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error during expired cart cleanup.");
            }
        }

        _logger.LogInformation("Expired cart cleanup service stopping.");
    }

    private async Task CleanupExpiredCartsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // First delete CartItems belonging to expired carts
        var deletedItems = await db.Database.ExecuteSqlRawAsync(
            @"DELETE ci FROM CartItems ci
              INNER JOIN Carts c ON ci.CartId = c.Id
              WHERE c.ExpiresAt IS NOT NULL AND c.ExpiresAt < GETUTCDATE()", ct);

        // Then delete the expired carts themselves
        var deletedCarts = await db.Database.ExecuteSqlRawAsync(
            @"DELETE FROM Carts 
              WHERE ExpiresAt IS NOT NULL AND ExpiresAt < GETUTCDATE()", ct);

        if (deletedCarts > 0)
        {
            _logger.LogInformation(
                "Cleaned up {CartCount} expired carts and {ItemCount} cart items.",
                deletedCarts, deletedItems);
        }
    }
}
