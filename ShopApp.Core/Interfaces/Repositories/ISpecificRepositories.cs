using ShopApp.Core.Entities;

namespace ShopApp.Core.Interfaces.Repositories;

public interface IItemRepository : IRepository<Item>
{
    Task<(IEnumerable<Item> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default);

    Task<IEnumerable<Item>> GetBySellerIdAsync(Guid sellerId, CancellationToken ct = default);
    Task<Item?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
}

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
    Task<Cart?> GetWithItemsAsync(Guid cartId, CancellationToken ct = default);
    Task AddCartItemAsync(CartItem item, CancellationToken ct = default);
    Task RemoveCartItemAsync(CartItem item, CancellationToken ct = default);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByBuyerIdAsync(Guid buyerId, CancellationToken ct = default);
    Task<Order?> GetWithDetailsAsync(Guid orderId, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveAsync(CancellationToken ct = default);
    Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IEnumerable<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task RevokeAllByUserIdAsync(Guid userId, string reason, CancellationToken ct = default);
}

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Payment?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
}

