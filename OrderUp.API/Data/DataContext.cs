using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data.Entities;

namespace OrderUp.API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<Addon> Addons => Set<Addon>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderItemAddon> OrderItemAddons => Set<OrderItemAddon>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ProductCategory -> Products (1-many)
            modelBuilder.Entity<ProductCategory>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product -> Variants (1-many)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Variants)
                .WithOne(v => v.Product)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order -> Items (1-many)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> OrderItemAddons (1-many)
            modelBuilder.Entity<OrderItem>()
                .HasMany(i => i.Addons)
                .WithOne(a => a.OrderItem)
                .HasForeignKey(a => a.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem -> Product (many-1, no cascade)
            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItem -> Variant (many-1, no cascade)
            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Variant)
                .WithMany()
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderItemAddon -> Addon (many-1, no cascade)
            modelBuilder.Entity<OrderItemAddon>()
                .HasOne(a => a.Addon)
                .WithMany()
                .HasForeignKey(a => a.AddonId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision (18,2) for money fields
            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Addon>()
                .Property(a => a.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.BaseUnitPriceSnapshot)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItemAddon>()
                .Property(a => a.UnitPriceSnapshot)
                .HasPrecision(18, 2);
        }
    }
}
