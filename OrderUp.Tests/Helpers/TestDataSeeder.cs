using OrderUp.API.Data;
using OrderUp.API.Data.Entities;

namespace OrderUp.Tests.Helpers;

public static class TestDataSeeder
{
    /// <summary>
    /// Seeds a category with one available product containing variants at different price points.
    /// </summary>
    public static (ProductCategory category, Product product, ProductVariant smallVariant, ProductVariant mediumVariant, ProductVariant largeVariant) SeedCategoryWithProductAndVariants(DataContext context)
    {
        var category = new ProductCategory
        {
            Id = 1,
            Name = "Coffee",
            DisplayOrder = 1
        };

        var product = new Product
        {
            Id = 1,
            CategoryId = 1,
            Name = "Latte",
            Description = "Espresso with steamed milk",
            IsAvailable = true
        };

        var smallVariant = new ProductVariant
        {
            Id = 1,
            ProductId = 1,
            Name = "Small",
            Price = 3.50m,
            IsDefault = false,
            IsAvailable = true
        };

        var mediumVariant = new ProductVariant
        {
            Id = 2,
            ProductId = 1,
            Name = "Medium",
            Price = 4.50m,
            IsDefault = true,
            IsAvailable = true
        };

        var largeVariant = new ProductVariant
        {
            Id = 3,
            ProductId = 1,
            Name = "Large",
            Price = 5.50m,
            IsDefault = false,
            IsAvailable = true
        };

        context.ProductCategories.Add(category);
        context.Products.Add(product);
        context.ProductVariants.AddRange(smallVariant, mediumVariant, largeVariant);
        context.SaveChanges();

        return (category, product, smallVariant, mediumVariant, largeVariant);
    }

    /// <summary>
    /// Seeds addons with different prices and groups.
    /// </summary>
    public static (Addon extraShot, Addon oatMilk, Addon vanilla) SeedAddons(DataContext context)
    {
        var extraShot = new Addon
        {
            Id = 1,
            Name = "Extra Shot",
            Price = 0.75m,
            Group = "Espresso",
            MaxPerItem = 3,
            IsAvailable = true
        };

        var oatMilk = new Addon
        {
            Id = 2,
            Name = "Oat Milk",
            Price = 0.60m,
            Group = "Milk Alternative",
            MaxPerItem = 1,
            IsAvailable = true
        };

        var vanilla = new Addon
        {
            Id = 3,
            Name = "Vanilla Syrup",
            Price = 0.50m,
            Group = "Syrups",
            MaxPerItem = 2,
            IsAvailable = true
        };

        context.Addons.AddRange(extraShot, oatMilk, vanilla);
        context.SaveChanges();

        return (extraShot, oatMilk, vanilla);
    }

    /// <summary>
    /// Seeds an unavailable product with variants.
    /// </summary>
    public static Product SeedUnavailableProduct(DataContext context)
    {
        var product = new Product
        {
            Id = 100,
            CategoryId = 1, // Assumes category 1 exists
            Name = "Seasonal Special",
            Description = "Currently unavailable",
            IsAvailable = false
        };

        var variant = new ProductVariant
        {
            Id = 100,
            ProductId = 100,
            Name = "Regular",
            Price = 6.00m,
            IsDefault = true,
            IsAvailable = true
        };

        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.SaveChanges();

        return product;
    }

    /// <summary>
    /// Seeds an available product with an unavailable variant.
    /// </summary>
    public static (Product product, ProductVariant availableVariant, ProductVariant unavailableVariant) SeedProductWithUnavailableVariant(DataContext context)
    {
        var product = new Product
        {
            Id = 200,
            CategoryId = 1,
            Name = "Mocha",
            Description = "Chocolate coffee",
            IsAvailable = true
        };

        var availableVariant = new ProductVariant
        {
            Id = 200,
            ProductId = 200,
            Name = "Medium",
            Price = 5.00m,
            IsDefault = true,
            IsAvailable = true
        };

        var unavailableVariant = new ProductVariant
        {
            Id = 201,
            ProductId = 200,
            Name = "Large",
            Price = 6.00m,
            IsDefault = false,
            IsAvailable = false
        };

        context.Products.Add(product);
        context.ProductVariants.AddRange(availableVariant, unavailableVariant);
        context.SaveChanges();

        return (product, availableVariant, unavailableVariant);
    }

    /// <summary>
    /// Seeds an unavailable addon.
    /// </summary>
    public static Addon SeedUnavailableAddon(DataContext context)
    {
        var addon = new Addon
        {
            Id = 100,
            Name = "Seasonal Pumpkin Spice",
            Price = 0.75m,
            Group = "Seasonal",
            MaxPerItem = 1,
            IsAvailable = false
        };

        context.Addons.Add(addon);
        context.SaveChanges();

        return addon;
    }

    /// <summary>
    /// Seeds a second category with products for menu tests.
    /// </summary>
    public static (ProductCategory category, Product product) SeedSecondCategory(DataContext context)
    {
        var category = new ProductCategory
        {
            Id = 2,
            Name = "Pastries",
            DisplayOrder = 2
        };

        var product = new Product
        {
            Id = 50,
            CategoryId = 2,
            Name = "Croissant",
            Description = "Buttery flaky pastry",
            IsAvailable = true
        };

        var variant = new ProductVariant
        {
            Id = 50,
            ProductId = 50,
            Name = "Regular",
            Price = 3.00m,
            IsDefault = true,
            IsAvailable = true
        };

        context.ProductCategories.Add(category);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);
        context.SaveChanges();

        return (category, product);
    }
}
