using ShopApp.Application.Common;
using ShopApp.Application.DTOs.Cart;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Entities;
using ShopApp.Core.Interfaces.Repositories;

namespace ShopApp.Application.Services;

/// <summary>
/// Cart works for both guests (session-based) and authenticated users.
/// On login, guest cart is merged into user cart via MergeGuestCartAsync.
/// TODO: Add cart expiry cleanup (background job).
/// </summary>
public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly IItemRepository _itemRepository;

    public CartService(ICartRepository cartRepository, IItemRepository itemRepository)
    {
        _cartRepository = cartRepository;
        _itemRepository = itemRepository;
    }

    public async Task<Result<CartDto>> GetCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId, ct);
        return Result<CartDto>.Success(MapToDto(cart));
    }

    public async Task<Result<CartDto>> AddItemAsync(Guid? userId, string? sessionId, AddToCartDto dto, CancellationToken ct = default)
    {
        var item = await _itemRepository.GetByIdAsync(dto.ItemId, ct);
        if (item is null) return Result<CartDto>.NotFound("Item not found.");
        if (item.Quantity < dto.Quantity) return Result<CartDto>.Failure("Insufficient stock.");

        var cart = await GetOrCreateCartAsync(userId, sessionId, ct);
        var existing = cart.Items.FirstOrDefault(ci => ci.ItemId == dto.ItemId);

        if (existing is not null)
            existing.Quantity += dto.Quantity;
        else
            cart.Items.Add(new CartItem { CartId = cart.Id, ItemId = dto.ItemId, Quantity = dto.Quantity });

        await _cartRepository.UpdateAsync(cart, ct);
        return Result<CartDto>.Success(MapToDto(cart));
    }

    public async Task<Result<CartDto>> UpdateItemAsync(Guid? userId, string? sessionId, Guid cartItemId, UpdateCartItemDto dto, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId, ct);
        var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
        if (cartItem is null) return Result<CartDto>.NotFound("Cart item not found.");

        if (dto.Quantity <= 0)
            cart.Items.Remove(cartItem);
        else
            cartItem.Quantity = dto.Quantity;

        await _cartRepository.UpdateAsync(cart, ct);
        return Result<CartDto>.Success(MapToDto(cart));
    }

    public async Task<r> RemoveItemAsync(Guid? userId, string? sessionId, Guid cartItemId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId, ct);
        var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);
        if (item is null) return Result.NotFound();
        cart.Items.Remove(item);
        await _cartRepository.UpdateAsync(cart, ct);
        return Result.Success();
    }

    public async Task<r> ClearCartAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId, ct);
        cart.Items.Clear();
        await _cartRepository.UpdateAsync(cart, ct);
        return Result.Success();
    }

    public async Task<r> MergeGuestCartAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        var guestCart = await _cartRepository.GetBySessionIdAsync(sessionId, ct);
        if (guestCart is null) return Result.Success();

        var userCart = await _cartRepository.GetByUserIdAsync(userId, ct) 
                       ?? new Cart { UserId = userId };

        foreach (var guestItem in guestCart.Items)
        {
            var existing = userCart.Items.FirstOrDefault(ci => ci.ItemId == guestItem.ItemId);
            if (existing is not null)
                existing.Quantity += guestItem.Quantity;
            else
                userCart.Items.Add(new CartItem { CartId = userCart.Id, ItemId = guestItem.ItemId, Quantity = guestItem.Quantity });
        }

        await _cartRepository.UpdateAsync(userCart, ct);
        await _cartRepository.DeleteAsync(guestCart, ct);
        return Result.Success();
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid? userId, string? sessionId, CancellationToken ct)
    {
        Cart? cart = null;
        if (userId.HasValue)
            cart = await _cartRepository.GetByUserIdAsync(userId.Value, ct);
        else if (sessionId is not null)
            cart = await _cartRepository.GetBySessionIdAsync(sessionId, ct);

        if (cart is null)
        {
            cart = new Cart { UserId = userId, SessionId = sessionId, ExpiresAt = DateTime.UtcNow.AddDays(30) };
            await _cartRepository.AddAsync(cart, ct);
        }
        return cart;
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.Items.Select(ci => new CartItemDto(
            ci.Id, ci.ItemId,
            ci.Item?.Title ?? "",
            ci.Item?.Photos.FirstOrDefault(p => p.IsPrimary)?.Url,
            ci.Item?.Price ?? 0,
            ci.Quantity,
            (ci.Item?.Price ?? 0) * ci.Quantity)).ToList();

        return new CartDto(cart.Id, items, items.Sum(i => i.SubTotal), items.Sum(i => i.Quantity));
    }
}
