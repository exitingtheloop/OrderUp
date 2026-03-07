using OrderUp.API.Data.Entities;
using OrderUp.API.Services.Admin;
using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Tests.Helpers;

namespace OrderUp.Tests.Services;

public class AdminMenuServiceTests
{
    #region Products - GetAllProductsAsync

    [Fact]
    public async Task GetAllProductsAsync_ReturnsAllProducts_IncludingUnavailable()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedUnavailableProduct(context);

        var service = new AdminMenuService(context);

        // Act
        var products = await service.GetAllProductsAsync();

        // Assert
        Assert.Equal(2, products.Count);
        Assert.Contains(products, p => p.Name == "Latte" && p.IsAvailable);
        Assert.Contains(products, p => p.Name == "Seasonal Special" && !p.IsAvailable);
    }

    [Fact]
    public async Task GetAllProductsAsync_IncludesAllVariants_RegardlessOfAvailability()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (product, _, unavailableVariant) = TestDataSeeder.SeedProductWithUnavailableVariant(context);

        var service = new AdminMenuService(context);

        // Act
        var products = await service.GetAllProductsAsync();

        // Assert
        var mocha = products.First(p => p.Name == "Mocha");
        Assert.Equal(2, mocha.Variants.Count);
        Assert.Contains(mocha.Variants, v => v.Name == "Medium" && v.IsAvailable);
        Assert.Contains(mocha.Variants, v => v.Name == "Large" && !v.IsAvailable);
    }

    [Fact]
    public async Task GetAllProductsAsync_ReturnsEmpty_WhenNoProducts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);

        // Act
        var products = await service.GetAllProductsAsync();

        // Assert
        Assert.Empty(products);
    }

    #endregion

    #region Products - GetProductAsync

    [Fact]
    public async Task GetProductAsync_ReturnsProduct_WithAllDetails()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (category, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new AdminMenuService(context);

        // Act
        var result = await service.GetProductAsync(product.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Latte", result.Name);
        Assert.Equal(3, result.Variants.Count);
        Assert.Equal(3, result.AllowedAddons.Count);
    }

    [Fact]
    public async Task GetProductAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);

        // Act
        var result = await service.GetProductAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Products - CreateProductAsync

    [Fact]
    public async Task CreateProductAsync_CreatesProduct_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var category = new ProductCategory { Id = 1, Name = "Coffee", DisplayOrder = 1 };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();

        var service = new AdminMenuService(context);
        var request = new CreateProductRequest(
            CategoryId: 1,
            Name: "Espresso",
            Description: "Strong coffee",
            ImageUrl: "espresso.jpg",
            IsAvailable: true
        );

        // Act
        var result = await service.CreateProductAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Espresso", result.Name);
        Assert.Equal("Strong coffee", result.Description);
        Assert.True(result.IsAvailable);
        Assert.Equal(1, result.CategoryId);
    }

    [Fact]
    public async Task CreateProductAsync_ThrowsException_WhenCategoryNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);
        var request = new CreateProductRequest(
            CategoryId: 999,
            Name: "Test",
            Description: null,
            ImageUrl: null,
            IsAvailable: true
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateProductAsync(request));
        Assert.Contains("Category 999 not found", ex.Message);
    }

    #endregion

    #region Products - UpdateProductAsync

    [Fact]
    public async Task UpdateProductAsync_UpdatesProduct_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (category, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);
        var request = new UpdateProductRequest(
            Name: "Updated Latte",
            Description: "Updated description",
            ImageUrl: "new.jpg",
            IsAvailable: false,
            CategoryId: category.Id
        );

        // Act
        var result = await service.UpdateProductAsync(product.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Latte", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task UpdateProductAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);
        var request = new UpdateProductRequest("Test", null, null, true, 1);

        // Act
        var result = await service.UpdateProductAsync(999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProductAsync_ThrowsException_WhenNewCategoryNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);
        var request = new UpdateProductRequest("Test", null, null, true, 999);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateProductAsync(product.Id, request));
        Assert.Contains("Category 999 not found", ex.Message);
    }

    #endregion

    #region Products - DeleteProductAsync

    [Fact]
    public async Task DeleteProductAsync_DeletesProduct_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);

        // Act
        var result = await service.DeleteProductAsync(product.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.Products.FindAsync(product.Id));
    }

    [Fact]
    public async Task DeleteProductAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);

        // Act
        var result = await service.DeleteProductAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProductAsync_ThrowsException_WhenHasOrders()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        // Create an order referencing this product
        var order = new Order
        {
            CustomerName = "Test",
            CustomerPhone = "123",
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };
        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            VariantId = mediumVariant.Id,
            Quantity = 1,
            BaseUnitPriceSnapshot = 4.50m,
            ProductNameSnapshot = "Latte",
            VariantNameSnapshot = "Medium"
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new AdminMenuService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteProductAsync(product.Id));
        Assert.Contains("has associated orders", ex.Message);
    }

    #endregion

    #region Variants - CreateVariantAsync

    [Fact]
    public async Task CreateVariantAsync_CreatesVariant_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);
        var request = new CreateProductVariantRequest(
            Name: "Extra Large",
            Price: 6.50m,
            IsDefault: false,
            IsAvailable: true
        );

        // Act
        var result = await service.CreateVariantAsync(product.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Extra Large", result.Name);
        Assert.Equal(6.50m, result.Price);
    }

    [Fact]
    public async Task CreateVariantAsync_ThrowsException_WhenProductNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);
        var request = new CreateProductVariantRequest("Test", 5.00m, false, true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateVariantAsync(999, request));
        Assert.Contains("Product 999 not found", ex.Message);
    }

    [Fact]
    public async Task CreateVariantAsync_UnsetsOtherDefaults_WhenNewIsDefault()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        Assert.True(mediumVariant.IsDefault);

        var service = new AdminMenuService(context);
        var request = new CreateProductVariantRequest(
            Name: "New Default",
            Price: 7.00m,
            IsDefault: true,
            IsAvailable: true
        );

        // Act
        var result = await service.CreateVariantAsync(product.Id, request);

        // Assert
        Assert.True(result.IsDefault);

        // Reload medium variant and check it's no longer default
        await context.Entry(mediumVariant).ReloadAsync();
        Assert.False(mediumVariant.IsDefault);
    }

    #endregion

    #region Variants - UpdateVariantAsync

    [Fact]
    public async Task UpdateVariantAsync_UpdatesVariant_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);
        var request = new UpdateProductVariantRequest(
            Name: "Tiny",
            Price: 2.50m,
            IsDefault: false,
            IsAvailable: false
        );

        // Act
        var result = await service.UpdateVariantAsync(product.Id, smallVariant.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tiny", result.Name);
        Assert.Equal(2.50m, result.Price);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task UpdateVariantAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);
        var request = new UpdateProductVariantRequest("Test", 5.00m, false, true);

        // Act
        var result = await service.UpdateVariantAsync(product.Id, 999, request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Variants - DeleteVariantAsync

    [Fact]
    public async Task DeleteVariantAsync_DeletesVariant_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);

        // Act
        var result = await service.DeleteVariantAsync(product.Id, smallVariant.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.ProductVariants.FindAsync(smallVariant.Id));
    }

    [Fact]
    public async Task DeleteVariantAsync_ThrowsException_WhenHasOrders()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        // Create an order referencing this variant
        var order = new Order
        {
            CustomerName = "Test",
            CustomerPhone = "123",
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };
        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            VariantId = mediumVariant.Id,
            Quantity = 1,
            BaseUnitPriceSnapshot = 4.50m,
            ProductNameSnapshot = "Latte",
            VariantNameSnapshot = "Medium"
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new AdminMenuService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteVariantAsync(product.Id, mediumVariant.Id));
        Assert.Contains("has associated orders", ex.Message);
    }

    #endregion

    #region ProductAddons - UpdateProductAddonsAsync

    [Fact]
    public async Task UpdateProductAddonsAsync_ReplacesAddons_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, oatMilk, vanilla) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new AdminMenuService(context);
        var request = new UpdateProductAddonsRequest([extraShot.Id]); // Only keep Extra Shot

        // Act
        var result = await service.UpdateProductAddonsAsync(product.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.AllowedAddons);
        Assert.Equal("Extra Shot", result.AllowedAddons[0].Name);
    }

    [Fact]
    public async Task UpdateProductAddonsAsync_ClearsAllAddons_WhenEmptyList()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new AdminMenuService(context);
        var request = new UpdateProductAddonsRequest([]);

        // Act
        var result = await service.UpdateProductAddonsAsync(product.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.AllowedAddons);
    }

    [Fact]
    public async Task UpdateProductAddonsAsync_ReturnsNull_WhenProductNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);
        var request = new UpdateProductAddonsRequest([1, 2]);

        // Act
        var result = await service.UpdateProductAddonsAsync(999, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProductAddonsAsync_ThrowsException_WhenAddonNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new AdminMenuService(context);
        var request = new UpdateProductAddonsRequest([999, 998]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateProductAddonsAsync(product.Id, request));
        Assert.Contains("Addon(s) not found", ex.Message);
    }

    #endregion

    #region Addons - GetAllAddonsAsync

    [Fact]
    public async Task GetAllAddonsAsync_ReturnsAllAddons_IncludingUnavailable()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedAddons(context);
        TestDataSeeder.SeedUnavailableAddon(context);

        var service = new AdminMenuService(context);

        // Act
        var addons = await service.GetAllAddonsAsync();

        // Assert
        Assert.Equal(4, addons.Count);
        Assert.Contains(addons, a => a.Name == "Seasonal Pumpkin Spice" && !a.IsAvailable);
    }

    #endregion

    #region Addons - CreateAddonAsync

    [Fact]
    public async Task CreateAddonAsync_CreatesAddon_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);
        var request = new CreateAddonRequest(
            Name: "Hazelnut Syrup",
            Price: 0.50m,
            Group: "Syrups",
            MaxPerItem: 2,
            IsAvailable: true
        );

        // Act
        var result = await service.CreateAddonAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hazelnut Syrup", result.Name);
        Assert.Equal(0.50m, result.Price);
        Assert.Equal("Syrups", result.Group);
        Assert.Equal(2, result.MaxPerItem);
    }

    #endregion

    #region Addons - UpdateAddonAsync

    [Fact]
    public async Task UpdateAddonAsync_UpdatesAddon_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (extraShot, _, _) = TestDataSeeder.SeedAddons(context);

        var service = new AdminMenuService(context);
        var request = new UpdateAddonRequest(
            Name: "Double Shot",
            Price: 1.00m,
            Group: "Espresso",
            MaxPerItem: 5,
            IsAvailable: false
        );

        // Act
        var result = await service.UpdateAddonAsync(extraShot.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Double Shot", result.Name);
        Assert.Equal(1.00m, result.Price);
        Assert.Equal(5, result.MaxPerItem);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task UpdateAddonAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);
        var request = new UpdateAddonRequest("Test", 1.00m, "Test", 1, true);

        // Act
        var result = await service.UpdateAddonAsync(999, request);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Addons - DeleteAddonAsync

    [Fact]
    public async Task DeleteAddonAsync_DeletesAddon_Successfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (extraShot, _, _) = TestDataSeeder.SeedAddons(context);

        var service = new AdminMenuService(context);

        // Act
        var result = await service.DeleteAddonAsync(extraShot.Id);

        // Assert
        Assert.True(result);
        Assert.Null(await context.Addons.FindAsync(extraShot.Id));
    }

    [Fact]
    public async Task DeleteAddonAsync_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new AdminMenuService(context);

        // Act
        var result = await service.DeleteAddonAsync(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAddonAsync_ThrowsException_WhenHasOrders()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, _, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        // Create an order with this addon
        var order = new Order
        {
            CustomerName = "Test",
            CustomerPhone = "123",
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };
        var orderItem = new OrderItem
        {
            ProductId = product.Id,
            VariantId = mediumVariant.Id,
            Quantity = 1,
            BaseUnitPriceSnapshot = 4.50m,
            ProductNameSnapshot = "Latte",
            VariantNameSnapshot = "Medium"
        };
        orderItem.Addons.Add(new OrderItemAddon
        {
            AddonId = extraShot.Id,
            Quantity = 1,
            UnitPriceSnapshot = 0.75m,
            NameSnapshot = "Extra Shot",
            GroupSnapshot = "Espresso"
        });
        order.Items.Add(orderItem);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new AdminMenuService(context);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteAddonAsync(extraShot.Id));
        Assert.Contains("has associated orders", ex.Message);
    }

    #endregion
}
