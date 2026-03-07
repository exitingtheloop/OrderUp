using OrderUp.API.Data.Entities;
using OrderUp.API.Services.Menu;
using OrderUp.Tests.Helpers;

namespace OrderUp.Tests.Services;

public class MenuServiceTests
{
    #region GetMenuAsync - Categories

    [Fact]
    public async Task GetMenuAsync_ReturnsAllCategories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedSecondCategory(context);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Equal(2, menu.Categories.Count);
        Assert.Contains(menu.Categories, c => c.Name == "Coffee");
        Assert.Contains(menu.Categories, c => c.Name == "Pastries");
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsCategoriesInDisplayOrder()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        // Add categories in non-display order
        context.ProductCategories.AddRange(
            new ProductCategory { Id = 1, Name = "Desserts", DisplayOrder = 3 },
            new ProductCategory { Id = 2, Name = "Coffee", DisplayOrder = 1 },
            new ProductCategory { Id = 3, Name = "Sandwiches", DisplayOrder = 2 }
        );
        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Equal(3, menu.Categories.Count);
        Assert.Equal("Coffee", menu.Categories[0].Name);
        Assert.Equal("Sandwiches", menu.Categories[1].Name);
        Assert.Equal("Desserts", menu.Categories[2].Name);
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsEmptyCategories_WhenNoneExist()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Empty(menu.Categories);
    }

    #endregion

    #region GetMenuAsync - Products Availability Filtering

    [Fact]
    public async Task GetMenuAsync_ReturnsOnlyAvailableProducts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context); // Available product
        TestDataSeeder.SeedUnavailableProduct(context); // Unavailable product

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Single(menu.Products);
        Assert.All(menu.Products, p => Assert.True(p.IsAvailable));
        Assert.DoesNotContain(menu.Products, p => p.Name == "Seasonal Special");
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsOnlyAvailableVariants_ForProducts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (product, availableVariant, unavailableVariant) = TestDataSeeder.SeedProductWithUnavailableVariant(context);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var mochaProduct = menu.Products.FirstOrDefault(p => p.Name == "Mocha");
        Assert.NotNull(mochaProduct);
        Assert.Single(mochaProduct.Variants);
        Assert.Equal("Medium", mochaProduct.Variants[0].Name);
        Assert.DoesNotContain(mochaProduct.Variants, v => v.Name == "Large");
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsEmptyProducts_WhenNoneAvailable()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);

        // Add only unavailable products
        var product = new Product
        {
            Id = 1,
            CategoryId = 1,
            Name = "Unavailable Latte",
            IsAvailable = false
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Empty(menu.Products);
    }

    #endregion

    #region GetMenuAsync - Product Details

    [Fact]
    public async Task GetMenuAsync_ReturnsProductDetails_Correctly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (category, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Single(menu.Products);
        var returnedProduct = menu.Products[0];

        Assert.Equal(product.Id, returnedProduct.Id);
        Assert.Equal("Latte", returnedProduct.Name);
        Assert.Equal("Espresso with steamed milk", returnedProduct.Description);
        Assert.True(returnedProduct.IsAvailable);
        Assert.Equal(category.Id, returnedProduct.CategoryId);
        Assert.Equal("Coffee", returnedProduct.CategoryName);
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsAllVariants_ForAvailableProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var product = menu.Products.First();
        Assert.Equal(3, product.Variants.Count);
        Assert.Contains(product.Variants, v => v.Name == "Small" && v.Price == 3.50m);
        Assert.Contains(product.Variants, v => v.Name == "Medium" && v.Price == 4.50m && v.IsDefault);
        Assert.Contains(product.Variants, v => v.Name == "Large" && v.Price == 5.50m);
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsVariantDetails_Correctly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var variant = menu.Products[0].Variants.First(v => v.Name == "Medium");

        Assert.Equal(2, variant.Id);
        Assert.Equal("Medium", variant.Name);
        Assert.Equal(4.50m, variant.Price);
        Assert.True(variant.IsDefault);
        Assert.True(variant.IsAvailable);
    }

    #endregion

    #region GetMenuAsync - Product AllowedAddons

    [Fact]
    public async Task GetMenuAsync_ReturnsOnlyAvailableAddons_ForProduct()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedAddonsForProduct(context, product.Id); // 3 available addons linked to product
        var unavailableAddon = TestDataSeeder.SeedUnavailableAddon(context);
        // Link unavailable addon to product too
        TestDataSeeder.SeedProductAddons(context, product.Id, unavailableAddon.Id);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var returnedProduct = menu.Products.First();
        Assert.Equal(3, returnedProduct.AllowedAddons.Count);
        Assert.All(returnedProduct.AllowedAddons, a => Assert.True(a.IsAvailable));
        Assert.DoesNotContain(returnedProduct.AllowedAddons, a => a.Name == "Seasonal Pumpkin Spice");
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsEmptyAllowedAddons_WhenNoneLinked()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedAddons(context); // Addons exist but not linked to product

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var product = menu.Products.First();
        Assert.Empty(product.AllowedAddons);
    }

    #endregion

    #region GetMenuAsync - Addon Details

    [Fact]
    public async Task GetMenuAsync_ReturnsAddonDetails_Correctly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var returnedProduct = menu.Products.First();
        Assert.Equal(3, returnedProduct.AllowedAddons.Count);

        var extraShot = returnedProduct.AllowedAddons.First(a => a.Name == "Extra Shot");
        Assert.Equal(1, extraShot.Id);
        Assert.Equal(0.75m, extraShot.Price);
        Assert.Equal("Espresso", extraShot.Group);
        Assert.Equal(3, extraShot.MaxPerItem);
        Assert.True(extraShot.IsAvailable);

        var oatMilk = returnedProduct.AllowedAddons.First(a => a.Name == "Oat Milk");
        Assert.Equal(0.60m, oatMilk.Price);
        Assert.Equal("Milk Alternative", oatMilk.Group);
        Assert.Equal(1, oatMilk.MaxPerItem);
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsAddonsGroupedByGroup()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var returnedProduct = menu.Products.First();
        var groups = returnedProduct.AllowedAddons.Select(a => a.Group).Distinct().ToList();
        Assert.Equal(3, groups.Count);
        Assert.Contains("Espresso", groups);
        Assert.Contains("Milk Alternative", groups);
        Assert.Contains("Syrups", groups);
    }

    #endregion

    #region GetMenuAsync - Full Menu Structure

    [Fact]
    public async Task GetMenuAsync_ReturnsCompleteMenuStructure()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product1, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (_, product2) = TestDataSeeder.SeedSecondCategory(context);
        TestDataSeeder.SeedAddonsForProduct(context, product1.Id);

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.NotNull(menu);
        Assert.NotNull(menu.Categories);
        Assert.NotNull(menu.Products);

        Assert.Equal(2, menu.Categories.Count);
        Assert.Equal(2, menu.Products.Count);

        // First product has addons
        var coffeeProduct = menu.Products.First(p => p.Name == "Latte");
        Assert.Equal(3, coffeeProduct.AllowedAddons.Count);

        // Second product has no addons linked
        var pastryProduct = menu.Products.First(p => p.Name == "Croissant");
        Assert.Empty(pastryProduct.AllowedAddons);
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsEmptyMenu_WhenDatabaseEmpty()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.NotNull(menu);
        Assert.Empty(menu.Categories);
        Assert.Empty(menu.Products);
    }

    #endregion

    #region GetMenuAsync - Product-Category Relationship

    [Fact]
    public async Task GetMenuAsync_ProductsHaveCorrectCategoryReferences()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context); // Coffee category
        TestDataSeeder.SeedSecondCategory(context); // Pastries category

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var coffeeProduct = menu.Products.First(p => p.Name == "Latte");
        Assert.Equal(1, coffeeProduct.CategoryId);
        Assert.Equal("Coffee", coffeeProduct.CategoryName);

        var pastryProduct = menu.Products.First(p => p.Name == "Croissant");
        Assert.Equal(2, pastryProduct.CategoryId);
        Assert.Equal("Pastries", pastryProduct.CategoryName);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetMenuAsync_HandlesProductWithNoAvailableVariants()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);

        var product = new Product
        {
            Id = 1,
            CategoryId = 1,
            Name = "Special Blend",
            IsAvailable = true
        };
        context.Products.Add(product);

        // Add only unavailable variants
        context.ProductVariants.Add(new ProductVariant
        {
            Id = 1,
            ProductId = 1,
            Name = "Regular",
            Price = 5.00m,
            IsAvailable = false
        });
        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert - Product is still returned but with no variants
        Assert.Single(menu.Products);
        Assert.Empty(menu.Products[0].Variants);
    }

    [Fact]
    public async Task GetMenuAsync_HandlesMultipleProductsInSameCategory()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);

        var products = new[]
        {
            new Product { Id = 1, CategoryId = 1, Name = "Latte", IsAvailable = true },
            new Product { Id = 2, CategoryId = 1, Name = "Cappuccino", IsAvailable = true },
            new Product { Id = 3, CategoryId = 1, Name = "Americano", IsAvailable = true }
        };
        context.Products.AddRange(products);

        var variants = new[]
        {
            new ProductVariant { Id = 1, ProductId = 1, Name = "Regular", Price = 4.00m, IsAvailable = true },
            new ProductVariant { Id = 2, ProductId = 2, Name = "Regular", Price = 3.50m, IsAvailable = true },
            new ProductVariant { Id = 3, ProductId = 3, Name = "Regular", Price = 3.00m, IsAvailable = true }
        };
        context.ProductVariants.AddRange(variants);
        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Equal(3, menu.Products.Count);
        Assert.All(menu.Products, p => Assert.Equal(1, p.CategoryId));
        Assert.All(menu.Products, p => Assert.Equal("Coffee", p.CategoryName));
    }

    [Fact]
    public async Task GetMenuAsync_HandlesAddonWithNullMaxPerItem()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);

        var product = new Product { Id = 1, CategoryId = 1, Name = "Latte", IsAvailable = true };
        context.Products.Add(product);

        var variant = new ProductVariant { Id = 1, ProductId = 1, Name = "Regular", Price = 4.00m, IsAvailable = true };
        context.ProductVariants.Add(variant);

        var addon = new Addon { Id = 1, Name = "Whipped Cream", Price = 0.50m, Group = "Toppings", MaxPerItem = null, IsAvailable = true };
        context.Addons.Add(addon);

        context.ProductAddons.Add(new ProductAddon { ProductId = 1, AddonId = 1 });
        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var returnedProduct = menu.Products.First();
        Assert.Single(returnedProduct.AllowedAddons);
        Assert.Null(returnedProduct.AllowedAddons[0].MaxPerItem);
    }

    [Fact]
    public async Task GetMenuAsync_HandlesMixedAvailabilityCorrectly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        // Categories
        context.ProductCategories.AddRange(
            new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 },
            new ProductCategory { Id = 2, Name = "Tea", DisplayOrder = 2 }
        );

        // Products - mix of available and unavailable
        context.Products.AddRange(
            new Product { Id = 1, CategoryId = 1, Name = "Latte", IsAvailable = true },
            new Product { Id = 2, CategoryId = 1, Name = "Old Blend", IsAvailable = false },
            new Product { Id = 3, CategoryId = 2, Name = "Green Tea", IsAvailable = true }
        );

        // Variants - mix of available and unavailable
        context.ProductVariants.AddRange(
            new ProductVariant { Id = 1, ProductId = 1, Name = "Small", Price = 3.00m, IsAvailable = true },
            new ProductVariant { Id = 2, ProductId = 1, Name = "Large", Price = 4.00m, IsAvailable = false },
            new ProductVariant { Id = 3, ProductId = 2, Name = "Regular", Price = 3.50m, IsAvailable = true },
            new ProductVariant { Id = 4, ProductId = 3, Name = "Regular", Price = 2.50m, IsAvailable = true }
        );

        // Addons - mix of available and unavailable
        context.Addons.AddRange(
            new Addon { Id = 1, Name = "Honey", Price = 0.25m, Group = "Sweeteners", IsAvailable = true },
            new Addon { Id = 2, Name = "Old Syrup", Price = 0.50m, Group = "Syrups", IsAvailable = false }
        );

        // Link both addons to Latte
        context.ProductAddons.AddRange(
            new ProductAddon { ProductId = 1, AddonId = 1 },
            new ProductAddon { ProductId = 1, AddonId = 2 }
        );

        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        Assert.Equal(2, menu.Categories.Count); // All categories shown
        Assert.Equal(2, menu.Products.Count); // Only available products

        var latte = menu.Products.First(p => p.Name == "Latte");
        Assert.Single(latte.Variants); // Only available variants
        Assert.Equal("Small", latte.Variants[0].Name);
        Assert.Single(latte.AllowedAddons); // Only available addons
        Assert.Equal("Honey", latte.AllowedAddons[0].Name);
    }

    [Fact]
    public async Task GetMenuAsync_DifferentProductsHaveDifferentAddons()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);

        var latte = new Product { Id = 1, CategoryId = 1, Name = "Latte", IsAvailable = true };
        var americano = new Product { Id = 2, CategoryId = 1, Name = "Americano", IsAvailable = true };
        context.Products.AddRange(latte, americano);

        context.ProductVariants.AddRange(
            new ProductVariant { Id = 1, ProductId = 1, Name = "Regular", Price = 4.00m, IsAvailable = true },
            new ProductVariant { Id = 2, ProductId = 2, Name = "Regular", Price = 3.00m, IsAvailable = true }
        );

        var extraShot = new Addon { Id = 1, Name = "Extra Shot", Price = 0.75m, Group = "Espresso", IsAvailable = true };
        var oatMilk = new Addon { Id = 2, Name = "Oat Milk", Price = 0.60m, Group = "Milk", IsAvailable = true };
        context.Addons.AddRange(extraShot, oatMilk);

        // Latte gets both addons
        context.ProductAddons.Add(new ProductAddon { ProductId = 1, AddonId = 1 });
        context.ProductAddons.Add(new ProductAddon { ProductId = 1, AddonId = 2 });

        // Americano gets only extra shot
        context.ProductAddons.Add(new ProductAddon { ProductId = 2, AddonId = 1 });

        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var latteProduct = menu.Products.First(p => p.Name == "Latte");
        var americanoProduct = menu.Products.First(p => p.Name == "Americano");

        Assert.Equal(2, latteProduct.AllowedAddons.Count);
        Assert.Single(americanoProduct.AllowedAddons);
        Assert.Equal("Extra Shot", americanoProduct.AllowedAddons[0].Name);
    }

    [Fact]
    public async Task GetMenuAsync_RespectsMaxPerItemOverride()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();

        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);

        var product = new Product { Id = 1, CategoryId = 1, Name = "Latte", IsAvailable = true };
        context.Products.Add(product);

        var variant = new ProductVariant { Id = 1, ProductId = 1, Name = "Regular", Price = 4.00m, IsAvailable = true };
        context.ProductVariants.Add(variant);

        var addon = new Addon { Id = 1, Name = "Extra Shot", Price = 0.75m, Group = "Espresso", MaxPerItem = 5, IsAvailable = true };
        context.Addons.Add(addon);

        // Override MaxPerItem for this product-addon combination
        context.ProductAddons.Add(new ProductAddon { ProductId = 1, AddonId = 1, MaxPerItemOverride = 2 });

        await context.SaveChangesAsync();

        var service = new MenuService(context);

        // Act
        var menu = await service.GetMenuAsync();

        // Assert
        var returnedProduct = menu.Products.First();
        Assert.Single(returnedProduct.AllowedAddons);
        Assert.Equal(2, returnedProduct.AllowedAddons[0].MaxPerItem); // Uses override, not default 5
    }

    #endregion
}
