using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopApp.Core.Entities;

namespace ShopApp.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemPhoto> ItemPhotos => Set<ItemPhoto>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── ApplicationUser ─────────────────────────────────────────────────
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            b.Property(u => u.AvatarUrl).HasMaxLength(500);
            b.Property(u => u.BanReason).HasMaxLength(1000);
        });

        // ── Category ────────────────────────────────────────────────────────
        builder.Entity<Category>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.Name).HasMaxLength(200).IsRequired();
            b.Property(c => c.Slug).HasMaxLength(200).IsRequired();
            b.HasIndex(c => c.Slug).IsUnique();
            b.HasOne(c => c.ParentCategory)
             .WithMany(c => c.SubCategories)
             .HasForeignKey(c => c.ParentCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Item ─────────────────────────────────────────────────────────────
        builder.Entity<Item>(b =>
        {
            b.HasKey(i => i.Id);
            b.Property(i => i.Title).HasMaxLength(300).IsRequired();
            b.Property(i => i.Description).HasMaxLength(5000).IsRequired();
            b.Property(i => i.Price).HasPrecision(18, 2);
            b.Property(i => i.Location).HasMaxLength(200);
            b.HasOne(i => i.Category)
             .WithMany(c => c.Items)
             .HasForeignKey(i => i.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(i => i.Seller)
             .WithMany(u => u.Items)
             .HasForeignKey(i => i.SellerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ItemPhoto ────────────────────────────────────────────────────────
        builder.Entity<ItemPhoto>(b =>
        {
            b.HasKey(p => p.Id);
            b.Property(p => p.Url).HasMaxLength(1000).IsRequired();
            b.Property(p => p.AltText).HasMaxLength(300);
            b.HasOne(p => p.Item)
             .WithMany(i => i.Photos)
             .HasForeignKey(p => p.ItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Cart ─────────────────────────────────────────────────────────────
        builder.Entity<Cart>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasIndex(c => c.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            b.HasOne(c => c.User)
             .WithOne(u => u.Cart)
             .HasForeignKey<Cart>(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CartItem>(b =>
        {
            b.HasKey(ci => ci.Id);
            b.HasOne(ci => ci.Cart)
             .WithMany(c => c.Items)
             .HasForeignKey(ci => ci.CartId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(ci => ci.Item)
             .WithMany(i => i.CartItems)
             .HasForeignKey(ci => ci.ItemId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Order ────────────────────────────────────────────────────────────
        builder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
            b.HasIndex(o => o.OrderNumber).IsUnique();
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.Property(o => o.ShippingFirstName).HasMaxLength(100);
            b.Property(o => o.ShippingLastName).HasMaxLength(100);
            b.Property(o => o.ShippingAddress).HasMaxLength(300);
            b.Property(o => o.ShippingCity).HasMaxLength(100);
            b.Property(o => o.ShippingPostalCode).HasMaxLength(20);
            b.Property(o => o.ShippingCountry).HasMaxLength(10);
            b.HasOne(o => o.Buyer)
             .WithMany(u => u.Orders)
             .HasForeignKey(o => o.BuyerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<OrderItem>(b =>
        {
            b.HasKey(oi => oi.Id);
            b.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            b.Property(oi => oi.ItemTitleSnapshot).HasMaxLength(300);
            b.HasOne(oi => oi.Order)
             .WithMany(o => o.Items)
             .HasForeignKey(oi => oi.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(oi => oi.Item)
             .WithMany(i => i.OrderItems)
             .HasForeignKey(oi => oi.ItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Rename Identity tables to cleaner names
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}
