using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHaat.Domain.Catalog;
using SmartHaat.Domain.Inventory;
using SmartHaat.Domain.Orders;
using SmartHaat.Domain.Pricing;
using SmartHaat.Infrastructure.Identity;

namespace SmartHaat.Infrastructure.Persistence;

public sealed class SmartHaatDbContext(DbContextOptions<SmartHaatDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<DeliveryZone> DeliveryZones => Set<DeliveryZone>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Category>(e =>
        {
            e.ToTable("Categories"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(140).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Description).HasMaxLength(500);
            e.HasOne(x => x.ParentCategory).WithMany(x => x.Children).HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Product>(e =>
        {
            e.ToTable("Products"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Sku).HasMaxLength(64).IsRequired(); e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(300).IsRequired(); e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Unit).HasMaxLength(32).IsRequired(); e.Property(x => x.Brand).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.ImageKey).HasMaxLength(100);
            e.Property(x => x.Price).HasPrecision(18, 2); e.Property(x => x.ReorderLevel).HasPrecision(18, 3);
            e.HasIndex(x => new { x.CategoryId, x.IsActive });
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ApplicationUser>(e => { e.Property(x => x.DisplayName).HasMaxLength(160); });
        builder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id); e.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
            e.Property(x => x.Version).IsRowVersion();
            e.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<InventoryBatch>(e =>
        {
            e.ToTable("InventoryBatches", t => t.HasCheckConstraint("CK_InventoryBatches_Quantities", "InitialQuantity >= 0 AND RemainingQuantity >= 0 AND ReservedQuantity >= 0 AND ReservedQuantity <= RemainingQuantity AND RemainingQuantity <= InitialQuantity"));
            e.HasKey(x => x.Id); e.Property(x => x.BatchNumber).HasMaxLength(80).IsRequired();
            e.Property(x => x.InitialQuantity).HasPrecision(18, 3); e.Property(x => x.RemainingQuantity).HasPrecision(18, 3); e.Property(x => x.ReservedQuantity).HasPrecision(18, 3);
            e.Property(x => x.UnitCost).HasPrecision(18, 2); e.Property(x => x.Version).IsRowVersion();
            e.HasIndex(x => new { x.ProductId, x.BatchNumber }).IsUnique(); e.HasIndex(x => new { x.ProductId, x.ExpiryDate, x.RemainingQuantity });
            e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockMovement>(e =>
        {
            e.ToTable("StockMovements"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.QuantityChange).HasPrecision(18, 3); e.Property(x => x.Notes).HasMaxLength(500);
            e.Property(x => x.Reason).HasConversion<string>().HasMaxLength(40);
            e.HasIndex(x => new { x.ProductId, x.CreatedAtUtc });
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<InventoryBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Cart>(e =>
        {
            e.ToTable("Carts"); e.HasKey(x => x.Id); e.Property(x => x.UserId).HasMaxLength(450); e.Property(x => x.GuestTokenHash).HasMaxLength(64);
            e.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL"); e.HasIndex(x => x.GuestTokenHash).IsUnique().HasFilter("[GuestTokenHash] IS NOT NULL");
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<CartItem>(e => { e.ToTable("CartItems"); e.HasKey(x => x.Id); e.Property(x => x.Quantity).HasPrecision(18, 3); e.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique(); });
        builder.Entity<Order>(e =>
        {
            e.ToTable("Orders"); e.HasKey(x => x.Id); e.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired(); e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.IdempotencyKey).HasMaxLength(100); e.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");
            e.Property(x => x.UserId).HasMaxLength(450); e.Property(x => x.GuestTrackingTokenHash).HasMaxLength(64);
            e.Property(x => x.CustomerName).HasMaxLength(160).IsRequired(); e.Property(x => x.CustomerPhone).HasMaxLength(40).IsRequired(); e.Property(x => x.CustomerEmail).HasMaxLength(256);
            e.Property(x => x.AddressLine).HasMaxLength(400).IsRequired(); e.Property(x => x.Area).HasMaxLength(120).IsRequired(); e.Property(x => x.DeliveryNotes).HasMaxLength(500);
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20); e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30); e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired(); e.Property(x => x.Subtotal).HasPrecision(18, 2); e.Property(x => x.DiscountTotal).HasPrecision(18, 2);
            e.Property(x => x.DeliveryFee).HasPrecision(18, 2); e.Property(x => x.TaxTotal).HasPrecision(18, 2); e.Property(x => x.Total).HasPrecision(18, 2);
            e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.History).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<OrderItem>(e =>
        {
            e.ToTable("OrderItems"); e.HasKey(x => x.Id); e.Property(x => x.SkuSnapshot).HasMaxLength(64).IsRequired(); e.Property(x => x.NameSnapshot).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitSnapshot).HasMaxLength(32).IsRequired(); e.Property(x => x.UnitPrice).HasPrecision(18, 2); e.Property(x => x.Quantity).HasPrecision(18, 3); e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<OrderStatusHistory>(e => { e.ToTable("OrderStatusHistory"); e.HasKey(x => x.Id); e.Property(x => x.Id).ValueGeneratedOnAdd(); e.Property(x => x.OldStatus).HasConversion<string>().HasMaxLength(30); e.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(30); e.Property(x => x.ActorUserId).HasMaxLength(450); e.Property(x => x.Reason).HasMaxLength(500); });
        builder.Entity<InventoryReservation>(e =>
        {
            e.ToTable("InventoryReservations"); e.HasKey(x => x.Id); e.Property(x => x.Quantity).HasPrecision(18, 3);
            e.HasIndex(x => new { x.OrderId, x.ReleasedAtUtc }); e.HasIndex(x => new { x.ExpiresAtUtc, x.ReleasedAtUtc });
            // OrderItem already cascades from Order, and reservations cascade from OrderItem.
            // Keep this direct FK non-cascading to avoid SQL Server multiple cascade paths.
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<OrderItem>().WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<InventoryBatch>().WithMany().HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<DeliveryZone>(e => { e.ToTable("DeliveryZones"); e.HasKey(x => x.Id); e.Property(x => x.Code).HasMaxLength(40).IsRequired(); e.Property(x => x.Name).HasMaxLength(120).IsRequired(); e.Property(x => x.Fee).HasPrecision(18, 2); e.HasIndex(x => x.Code).IsUnique(); });
        builder.Entity<Coupon>(e => { e.ToTable("Coupons"); e.HasKey(x => x.Id); e.Property(x => x.Code).HasMaxLength(40).IsRequired(); e.HasIndex(x => x.Code).IsUnique(); e.Property(x => x.DiscountType).HasConversion<string>().HasMaxLength(20); e.Property(x => x.Value).HasPrecision(18, 2); e.Property(x => x.MinimumOrderAmount).HasPrecision(18, 2); e.Property(x => x.MaximumDiscount).HasPrecision(18, 2); });
        builder.Entity<CouponRedemption>(e => { e.ToTable("CouponRedemptions"); e.HasKey(x => x.Id); e.Property(x => x.CustomerUserId).HasMaxLength(450); e.Property(x => x.DiscountAmount).HasPrecision(18, 2); e.HasIndex(x => x.OrderId).IsUnique(); e.HasIndex(x => new { x.CouponId, x.CustomerUserId }); e.HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict); e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict); });
        builder.Entity<Order>(e => { e.Property(x => x.CouponCodeSnapshot).HasMaxLength(40); });
    }
}
