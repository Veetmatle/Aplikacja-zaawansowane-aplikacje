using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopApp.Core.Interfaces.Services;
using ShopApp.Infrastructure.Data;

namespace ShopApp.Infrastructure.Services;

/// <summary>
/// Bounded channel for buffering view count increments.
/// Implements IViewCountTracker for fire-and-forget tracking from ItemService.
/// </summary>
public sealed class ViewCountChannel : IViewCountTracker
{
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(10_000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ChannelReader<Guid> Reader => _channel.Reader;

    public void Track(Guid itemId)
    {
        // Fire-and-forget — non-blocking write
        _channel.Writer.TryWrite(itemId);
    }
}

/// <summary>
/// Background service that reads item IDs from the ViewCountChannel,
/// batches them, and flushes view count increments to the database
/// using raw SQL to avoid loading/tracking entities.
/// </summary>
public sealed class ViewCountBackgroundService : BackgroundService
{
    private readonly ViewCountChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ViewCountBackgroundService> _logger;

    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);
    private const int MaxBatchSize = 200;

    public ViewCountBackgroundService(
        ViewCountChannel channel,
        IServiceScopeFactory scopeFactory,
        ILogger<ViewCountBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ViewCount background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = new ConcurrentDictionary<Guid, int>();

            // Drain channel until batch is full or timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(FlushInterval);

            try
            {
                while (batch.Count < MaxBatchSize)
                {
                    var itemId = await _channel.Reader.ReadAsync(cts.Token);
                    batch.AddOrUpdate(itemId, 1, (_, count) => count + 1);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout or shutdown — flush what we have
            }

            if (batch.IsEmpty)
                continue;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                foreach (var (itemId, count) in batch)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE Items SET ViewCount = ViewCount + {count} WHERE Id = {itemId} AND DeletedAt IS NULL",
                        stoppingToken);
                }

                _logger.LogDebug("Flushed view counts for {Count} items.", batch.Count);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error flushing view counts for {Count} items.", batch.Count);
            }
        }

        _logger.LogInformation("ViewCount background service stopping.");
    }
}
