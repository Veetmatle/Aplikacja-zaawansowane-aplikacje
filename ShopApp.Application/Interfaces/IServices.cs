using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Auth;
using ShopApp.Application.DTOs.Cart;
using ShopApp.Application.DTOs.Item;
using ShopApp.Application.DTOs.Order;
using ShopApp.Application.DTOs.Payment;
using ShopApp.Application.DTOs.User;

namespace ShopApp.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default);
}

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateProfileAsync(Guid userId, UpdateUserDto dto, CancellationToken ct = default);
    Task<Result<PagedResult<UserDto>>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
}

public interface IAdminUserService
{
    Task<Result<UserDto>> GetUserDetailsAsync(Guid userId, CancellationToken ct = default);
    Task<Result<PagedResult<UserDto>>> GetAllUsersAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<Result> BanUserAsync(Guid userId, BanUserDto dto, CancellationToken ct = default);
    Task<Result> UnbanUserAsync(Guid userId, CancellationToken ct = default);
    Task<Result> SetTimeoutAsync(Guid userId, SetTimeoutDto dto, CancellationToken ct = default);
    Task<Result> RemoveTimeoutAsync(Guid userId, CancellationToken ct = default);
    Task<Result> AssignRoleAsync(Guid userId, AssignRoleDto dto, CancellationToken ct = default);
    Task<Result> RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken ct = default);
}

public interface IItemService
{
    Task<Result<PagedResult<ItemSummaryDto>>> GetItemsAsync(ItemQueryDto query, CancellationToken ct = default);
    Task<Result<ItemDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ItemDto>> CreateAsync(Guid sellerId, CreateItemDto dto, CancellationToken ct = default);
    Task<Result<ItemDto>> UpdateAsync(Guid itemId, Guid requestingUserId, UpdateItemDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid itemId, Guid requestingUserId, CancellationToken ct = default);
    Task<Result<IEnumerable<ItemSummaryDto>>> GetMyItemsAsync(Guid sellerId, CancellationToken ct = default);
    Task<Result<IEnumerable<ItemPhotoDto>>> UploadPhotosAsync(Guid itemId, Guid requestingUserId, IEnumerable<(Stream Stream, string FileName, string ContentType, long Size)> files, CancellationToken ct = default);
}

public interface ICartService
{
    Task<Result<CartDto>> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default);
    Task<Result<CartDto>> AddItemAsync(Guid? userId, string? sessionId, AddToCartDto dto, CancellationToken ct = default);
    Task<Result<CartDto>> UpdateItemAsync(Guid? userId, string? sessionId, Guid cartItemId, UpdateCartItemDto dto, CancellationToken ct = default);
    Task<Result> RemoveItemAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken ct = default);
    Task<Result> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default);
    Task<Result> MergeGuestCartAsync(Guid userId, string sessionId, CancellationToken ct = default);
}

public interface IOrderService
{
    Task<Result<OrderDto>> CreateFromCartAsync(Guid userId, CreateOrderDto dto, CancellationToken ct = default);
    Task<Result<OrderDto>> GetByIdAsync(Guid orderId, Guid requestingUserId, CancellationToken ct = default);
    Task<Result<IEnumerable<OrderDto>>> GetMyOrdersAsync(Guid userId, CancellationToken ct = default);
    Task<Result<OrderDto>> UpdateStatusAsync(Guid orderId, UpdateOrderStatusDto dto, CancellationToken ct = default);
}

public interface ICategoryService
{
    Task<Result<IEnumerable<CategoryDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<CategoryDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default);
    Task<Result<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IChatbotService
{
    Task<Result<string>> AskAsync(string question, string? context = null, CancellationToken ct = default);
}

public interface IPaymentService
{
    Task<Result<PaymentStatusDto>> InitiatePaymentAsync(Guid orderId, Guid userId, CancellationToken ct = default);
    Task<Result> HandleNotificationAsync(P24NotificationDto notification, CancellationToken ct = default);
    Task<Result<PaymentStatusDto>> GetPaymentStatusAsync(Guid orderId, Guid userId, CancellationToken ct = default);
}

