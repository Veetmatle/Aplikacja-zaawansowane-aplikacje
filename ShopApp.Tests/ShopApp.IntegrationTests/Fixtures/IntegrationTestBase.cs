using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopApp.Core.Interfaces.Services;
using ShopApp.Infrastructure.Data;
using Testcontainers.MsSql;

namespace ShopApp.IntegrationTests.Fixtures;

/// <summary>
/// Integration test base using Testcontainers for real MSSQL Server.
/// Each test class gets a fresh database via MsSqlContainer.
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor is not null)
                        services.Remove(descriptor);

                    // Add test DbContext with Testcontainer connection string
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(_dbContainer.GetConnectionString()));

                    // Replace payment gateway with a fake
                    var paymentDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IPaymentGateway));
                    if (paymentDescriptor is not null)
                        services.Remove(paymentDescriptor);
                    services.AddScoped<IPaymentGateway, FakePaymentGatewayForIntegration>();

                    // Apply migrations
                    using var scope = services.BuildServiceProvider().CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                });
            });

        Client = Factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        await _dbContainer.DisposeAsync();
    }
}

/// <summary>Simple fake payment gateway for integration tests.</summary>
internal class FakePaymentGatewayForIntegration : IPaymentGateway
{
    public Task<PaymentInitResult> InitializePaymentAsync(PaymentRequest request, CancellationToken ct = default)
        => Task.FromResult(new PaymentInitResult(true, "https://fake-p24/pay/test", "test-token"));

    public Task<bool> VerifyNotificationAsync(PaymentNotification notification, CancellationToken ct = default)
        => Task.FromResult(true);
}
