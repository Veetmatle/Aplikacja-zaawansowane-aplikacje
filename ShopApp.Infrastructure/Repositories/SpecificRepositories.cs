using Microsoft.EntityFrameworkCore;
using ShopApp.Core.Entities;
using ShopApp.Core.Enums;
using ShopApp.Core.Interfaces.Repositories;
using ShopApp.Infrastructure.Data;

namespace ShopApp.Infrastructure.Repositories;

public class ItemRepository : Repository<Item>, IItemRepository
{
    public ItemRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Item> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Guid? categoryId = null,
        string? searchQuery = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(i => i.Category)
            .Include(i => i.Seller)
            .Include(i => i.Photos)
            .Where(i => i.Status == ItemStatus.Active)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.Where(i => i.Title.Contains(searchQuery) || i.Description.Contains(searchQuery));

        if (minPrice.HasValue) query = query.Where(i => i.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(i => i.Price <= maxPrice.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IEnumerable<Item>> GetBySellerIdAsync(Guid sellerId, CancellationToken ct = default)
        => await _dbSet
            .Include(i => i.Category)
            .Include(i => i.Photos)
            .Where(i => i.SellerId == sellerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<Item?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(i => i.Category)
            .Include(i => i.Seller)
            .Include(i => i.Photos)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
}

public class CartRepository : Repository<Cart>, ICartRepository
{
    public CartRepository(AppDbContext context) : base(context) { }

    public async Task<Cart?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Items).ThenInclude(ci => ci.Item).ThenInclude(i => i.Photos)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

    public async Task<Cart?> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Items).ThenInclude(ci => ci.Item).ThenInclude(i => i.Photos)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

    public async Task<Cart?> GetWithItemsAsync(Guid cartId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Items).ThenInclude(ci => ci.Item)
            .FirstOrDefaultAsync(c => c.Id == cartId, ct);
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Order>> GetByBuyerIdAsync(Guid buyerId, CancellationToken ct = default)
        => await _dbSet
            .Include(o => o.Items)
            .Where(o => o.BuyerId == buyerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<Order?> GetWithDetailsAsync(Guid orderId, CancellationToken ct = default)
        => await _dbSet
            .Include(o => o.Items).ThenInclude(oi => oi.Item)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
        => await _dbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
}

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Category>> GetActiveAsync(CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.ParentCategory)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.ParentCategory)
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);
}
