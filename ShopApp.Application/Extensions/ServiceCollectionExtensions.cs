using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShopApp.Application.Interfaces;
using ShopApp.Application.Services;

namespace ShopApp.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register validators
        services.AddValidatorsFromAssemblyContaining<IAuthService>();

        // Register AutoMapper
        services.AddAutoMapper(typeof(IAuthService).Assembly);

        // Register application services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IChatbotService, ChatbotService>();

        return services;
    }
}
