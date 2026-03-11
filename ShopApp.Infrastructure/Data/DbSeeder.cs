using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopApp.Core.Entities;

namespace ShopApp.Infrastructure.Data;

public static class DbSeeder
{
    public static readonly string[] Roles = { "Admin", "User" };

    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        // NOTE: Migrations are applied separately via:
        //   dotnet run --migrate
        //   or: dotnet ef database update
        // Do NOT call context.Database.MigrateAsync() here — race condition risk with multiple instances.

        // Seed roles
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role) { Description = $"Default {role} role" });
        }

        // Seed admin user
        const string adminEmail = "admin@shopapp.local";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "ShopApp",
            };
            var result = await userManager.CreateAsync(admin, "Admin@1234!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // Seed default categories
        if (!await context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Electronics", Slug = "electronics", Description = "Phones, computers, gadgets" },
                new Category { Name = "Clothing", Slug = "clothing", Description = "Fashion and apparel" },
                new Category { Name = "Home & Garden", Slug = "home-garden", Description = "Furniture, tools, plants" },
                new Category { Name = "Sports", Slug = "sports", Description = "Sporting goods and equipment" },
                new Category { Name = "Books", Slug = "books", Description = "Books, comics, magazines" },
                new Category { Name = "Vehicles", Slug = "vehicles", Description = "Cars, motorcycles, parts" },
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }
    }
}
