using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data.Entities;

namespace OrderUp.API.Data.Seed;

public static class MenuSeeder
{
    public static async Task SeedAsync(DataContext context)
    {
        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
        await SeedAddonsAsync(context);
        await SeedProductAddonsAsync(context);
    }

    private static async Task SeedCategoriesAsync(DataContext context)
    {
        if (await context.ProductCategories.AnyAsync())
            return;

        var categories = new List<ProductCategory>
        {
            new() { Name = "Coffee", DisplayOrder = 1 },
            new() { Name = "Non-Coffee", DisplayOrder = 2 },
            new() { Name = "Pastries", DisplayOrder = 3 }
        };

        context.ProductCategories.AddRange(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(DataContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var coffeeCategory = await context.ProductCategories.FirstAsync(c => c.Name == "Coffee");
        var nonCoffeeCategory = await context.ProductCategories.FirstAsync(c => c.Name == "Non-Coffee");
        var pastriesCategory = await context.ProductCategories.FirstAsync(c => c.Name == "Pastries");

        var products = new List<Product>
        {
            // Coffee
            new()
            {
                CategoryId = coffeeCategory.Id,
                Name = "Americano",
                Description = "Espresso with hot water",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Small", Price = 95.00m, IsDefault = true, IsAvailable = true },
                    new() { Name = "Medium", Price = 115.00m, IsDefault = false, IsAvailable = true },
                    new() { Name = "Large", Price = 135.00m, IsDefault = false, IsAvailable = true }
                }
            },
            new()
            {
                CategoryId = coffeeCategory.Id,
                Name = "Cafe Latte",
                Description = "Espresso with steamed milk",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Small", Price = 110.00m, IsDefault = true, IsAvailable = true },
                    new() { Name = "Medium", Price = 130.00m, IsDefault = false, IsAvailable = true },
                    new() { Name = "Large", Price = 150.00m, IsDefault = false, IsAvailable = true }
                }
            },
            new()
            {
                CategoryId = coffeeCategory.Id,
                Name = "Cappuccino",
                Description = "Espresso with steamed milk foam",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Small", Price = 110.00m, IsDefault = true, IsAvailable = true },
                    new() { Name = "Medium", Price = 130.00m, IsDefault = false, IsAvailable = true },
                    new() { Name = "Large", Price = 150.00m, IsDefault = false, IsAvailable = true }
                }
            },

            // Non-Coffee
            new()
            {
                CategoryId = nonCoffeeCategory.Id,
                Name = "Matcha Latte",
                Description = "Japanese green tea with steamed milk",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Small", Price = 120.00m, IsDefault = true, IsAvailable = true },
                    new() { Name = "Medium", Price = 140.00m, IsDefault = false, IsAvailable = true },
                    new() { Name = "Large", Price = 160.00m, IsDefault = false, IsAvailable = true }
                }
            },
            new()
            {
                CategoryId = nonCoffeeCategory.Id,
                Name = "Hot Chocolate",
                Description = "Rich chocolate with steamed milk",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Small", Price = 100.00m, IsDefault = true, IsAvailable = true },
                    new() { Name = "Medium", Price = 120.00m, IsDefault = false, IsAvailable = true },
                    new() { Name = "Large", Price = 140.00m, IsDefault = false, IsAvailable = true }
                }
            },

            // Pastries
            new()
            {
                CategoryId = pastriesCategory.Id,
                Name = "Croissant",
                Description = "Buttery French pastry",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Regular", Price = 85.00m, IsDefault = true, IsAvailable = true }
                }
            },
            new()
            {
                CategoryId = pastriesCategory.Id,
                Name = "Chocolate Muffin",
                Description = "Rich chocolate muffin",
                IsAvailable = true,
                Variants = new List<ProductVariant>
                {
                    new() { Name = "Regular", Price = 75.00m, IsDefault = true, IsAvailable = true }
                }
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAddonsAsync(DataContext context)
    {
        if (await context.Addons.AnyAsync())
            return;

        var addons = new List<Addon>
        {
            new() { Name = "Extra Shot", Price = 30.00m, Group = "Espresso", MaxPerItem = 3, IsAvailable = true },
            new() { Name = "Oat Milk", Price = 25.00m, Group = "Milk", MaxPerItem = 1, IsAvailable = true },
            new() { Name = "Vanilla Syrup", Price = 20.00m, Group = "Syrup", MaxPerItem = 3, IsAvailable = true },
            new() { Name = "Caramel Syrup", Price = 20.00m, Group = "Syrup", MaxPerItem = 3, IsAvailable = true },
            new() { Name = "Whipped Cream", Price = 15.00m, Group = "Toppings", MaxPerItem = 1, IsAvailable = true }
        };

        context.Addons.AddRange(addons);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductAddonsAsync(DataContext context)
    {
        // Load all products with their categories
        var products = await context.Products
            .Include(p => p.Category)
            .ToListAsync();

        // Load all addons
        var addons = await context.Addons.ToListAsync();

        // Load existing mappings to ensure idempotency
        var existingMappingsList = await context.ProductAddons
            .Select(pa => new { pa.ProductId, pa.AddonId })
            .ToListAsync();
        var existingMappings = existingMappingsList
            .Select(x => (x.ProductId, x.AddonId))
            .ToHashSet();

        // Define addon eligibility by group per category
        // Coffee: all addons (Espresso, Milk, Syrup, Toppings)
        // Non-Coffee: Milk, Syrup, Toppings (no Espresso/Extra Shot)
        // Pastries: Toppings only

        var coffeeGroups = new HashSet<string> { "Espresso", "Milk", "Syrup", "Toppings" };
        var nonCoffeeGroups = new HashSet<string> { "Milk", "Syrup", "Toppings" };
        var pastryGroups = new HashSet<string> { "Toppings" };

        var productAddonsToAdd = new List<ProductAddon>();

        foreach (var product in products)
        {
            var allowedGroups = product.Category.Name switch
            {
                "Coffee" => coffeeGroups,
                "Non-Coffee" => nonCoffeeGroups,
                "Pastries" => pastryGroups,
                _ => new HashSet<string>() // Unknown categories get no addons
            };

            foreach (var addon in addons)
            {
                // Skip if addon group is not allowed for this category
                if (!allowedGroups.Contains(addon.Group))
                    continue;

                // Skip if mapping already exists (idempotency)
                if (existingMappings.Contains((product.Id, addon.Id)))
                    continue;

                productAddonsToAdd.Add(new ProductAddon
                {
                    ProductId = product.Id,
                    AddonId = addon.Id
                });
            }
        }

        if (productAddonsToAdd.Count > 0)
        {
            context.ProductAddons.AddRange(productAddonsToAdd);
            await context.SaveChangesAsync();
        }
    }
}
