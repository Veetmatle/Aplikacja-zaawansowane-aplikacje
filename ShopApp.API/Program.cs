using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShopApp.API.Extensions;
using ShopApp.API.Filters;
using ShopApp.Application.Extensions;
using ShopApp.Core.Entities;
using ShopApp.Infrastructure.Data;
using ShopApp.Infrastructure.Extensions;

// ── Serilog bootstrap ──────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ShopApp")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/shopapp-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

    // ── Configuration ──────────────────────────────────────────────────────
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables();

    // ── Services ───────────────────────────────────────────────────────────
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerWithJwt();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    // ── Health Checks ──────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection")!,
            name: "sqlserver",
            timeout: TimeSpan.FromSeconds(5),
            tags: new[] { "db", "ready" });

    // ── Rate Limiting ──────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter("chatbot", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 0;
        });

        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsync(
                """{"error":"Too many requests. Please try again later."}""", ct);
        };
    });

    // ── Build ──────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── CLI: run migrations only ───────────────────────────────────────────
    if (args.Contains("--migrate"))
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        Log.Information("Migrations applied successfully.");
        return;
    }

    // ── Seed database (roles, admin, categories — NO migrations) ───────────
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        await DbSeeder.SeedAsync(context, userManager, roleManager);
    }

    // ── Middleware pipeline ────────────────────────────────────────────────
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ShopApp API v1"));
    }

    app.UseCors("AllowAll");
    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<ShopApp.API.Middleware.ExceptionMiddleware>();

    // ── Health Checks endpoint ─────────────────────────────────────────────
    app.MapHealthChecks("/health");

    app.MapControllers();

    Log.Information("ShopApp API starting up...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
