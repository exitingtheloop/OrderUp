using OrderUp.API.Data.Entities;
using OrderUp.API.Services.Orders;
using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Tests.Helpers;

namespace OrderUp.Tests.Services;

public class OrderServiceTests
{
    #region CreateOrderAsync - Basic Order Creation

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_CreatesOrderSuccessfully()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "John Doe",
            CustomerPhone: "555-1234",
            PickupTimeUtc: DateTime.UtcNow.AddHours(1),
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: product.Id,
                    VariantId: mediumVariant.Id,
                    Quantity: 2,
                    Notes: "Extra hot",
                    Addons: []
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        Assert.True(response.OrderId > 0);

        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal("John Doe", order.CustomerName);
        Assert.Equal("555-1234", order.CustomerPhone);
        Assert.Equal(Shared.Enums.OrderStatus.Pending, order.Status);
        Assert.Equal(Shared.Enums.PaymentStatus.Pending, order.PaymentStatus);
        Assert.Single(order.Items);
    }

    [Fact]
    public async Task CreateOrderAsync_WithMultipleItems_CreatesAllItems()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, mediumVariant, largeVariant) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Jane Doe",
            CustomerPhone: "555-5678",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, smallVariant.Id, 1, null, []),
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 2, "No foam", []),
                new CreateOrderItemRequest(product.Id, largeVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(3, order.Items.Count);
    }

    #endregion

    #region CreateOrderAsync - Variant Pricing Snapshots

    [Fact]
    public async Task CreateOrderAsync_SnapshotsVariantPrice_AtOrderTime()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var originalPrice = mediumVariant.Price;

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Price Test",
            CustomerPhone: "555-0000",
            PickupTimeUtc: null,
            Items:
            [
               new CreateOrderItemRequest(product.Id, mediumVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Simulate price change after order
        mediumVariant.Price = 99.99m;
        await context.SaveChangesAsync();

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(originalPrice, order.Items[0].BaseUnitPrice);
    }

    [Fact]
    public async Task CreateOrderAsync_SnapshotsProductAndVariantNames()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Name Test",
            CustomerPhone: "555-0001",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, smallVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal("Latte", order.Items[0].ProductName);
        Assert.Equal("Small", order.Items[0].VariantName);
    }

    [Theory]
    [InlineData(1, 3.50)] // Small
    [InlineData(2, 4.50)] // Medium
    [InlineData(3, 5.50)] // Large
    public async Task CreateOrderAsync_UsesCorrectVariantPrice(int variantId, decimal expectedPrice)
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Variant Price Test",
            CustomerPhone: "555-0002",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, variantId, 1, null, [])
            ]
          );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(expectedPrice, order.Items[0].BaseUnitPrice);
    }

    #endregion

    #region CreateOrderAsync - Addon Pricing and Snapshots

    [Fact]
    public async Task CreateOrderAsync_WithAddons_SnapshotsAddonDetails()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, oatMilk, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Addon Test",
            CustomerPhone: "555-0003",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 2),
                        new CreateOrderItemAddonRequest(oatMilk.Id, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(2, order.Items[0].Addons.Count);

        var extraShotAddon = order.Items[0].Addons.First(a => a.Name == "Extra Shot");
        Assert.Equal(0.75m, extraShotAddon.UnitPrice);
        Assert.Equal(2, extraShotAddon.Quantity);
        Assert.Equal("Espresso", extraShotAddon.Group);

        var oatMilkAddon = order.Items[0].Addons.First(a => a.Name == "Oat Milk");
        Assert.Equal(0.60m, oatMilkAddon.UnitPrice);
        Assert.Equal(1, oatMilkAddon.Quantity);
        Assert.Equal("Milk Alternative", oatMilkAddon.Group);
    }

    [Fact]
    public async Task CreateOrderAsync_WithAddons_SnapshotsAddonPriceAtOrderTime()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, _, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);
        var originalPrice = extraShot.Price;

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Addon Price Test",
            CustomerPhone: "555-0004",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Simulate price change after order
        extraShot.Price = 99.99m;
        await context.SaveChangesAsync();

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(originalPrice, order.Items[0].Addons[0].UnitPrice);
    }

    #endregion

    #region CreateOrderAsync - Order Total Calculation

    [Fact]
    public async Task CreateOrderAsync_CalculatesTotal_ForSingleItemNoAddons()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        // Medium = $4.50, Qty = 2 => $9.00

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Total Test 1",
            CustomerPhone: "555-0005",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 2, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(9.00m, order.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_CalculatesTotal_WithAddons()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, oatMilk, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        // Medium = $4.50
        // Extra Shot x2 = $0.75 * 2 = $1.50
        // Oat Milk x1 = $0.60
        // Item total = $4.50 + $1.50 + $0.60 = $6.60
        // Quantity = 1 => Order total = $6.60

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Total Test 2",
            CustomerPhone: "555-0006",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 2),
                        new CreateOrderItemAddonRequest(oatMilk.Id, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(6.60m, order.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_CalculatesTotal_WithMultipleItemsAndAddons()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, _, vanilla) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        // Item 1: Small ($3.50) + Extra Shot x1 ($0.75) = $4.25, Qty 2 => $8.50
        // Item 2: Medium ($4.50) + Vanilla x2 ($0.50 * 2 = $1.00) = $5.50, Qty 1 => $5.50
        // Total = $8.50 + $5.50 = $14.00

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Total Test 3",
            CustomerPhone: "555-0007",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    smallVariant.Id,
                    2,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                ),
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(vanilla.Id, 2)
                    ]
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(14.00m, order.Total);
    }

    #endregion

    #region CreateOrderAsync - Validation Failures

    [Fact]
    public async Task CreateOrderAsync_WithZeroQuantity_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Invalid Qty",
            CustomerPhone: "555-0008",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 0, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("quantity must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNegativeQuantity_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Negative Qty",
            CustomerPhone: "555-0009",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, -1, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("quantity must be greater than 0", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentProduct_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, _, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Missing Product",
            CustomerPhone: "555-0010",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(999, mediumVariant.Id, 1, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("Product 999 not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithUnavailableProduct_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var unavailableProduct = TestDataSeeder.SeedUnavailableProduct(context);
        var variant = context.ProductVariants.First(v => v.ProductId == unavailableProduct.Id);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Unavailable Product",
            CustomerPhone: "555-0011",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(unavailableProduct.Id, variant.Id, 1, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("not available", exception.Message);
        Assert.Contains(unavailableProduct.Name, exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentVariant_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Missing Variant",
            CustomerPhone: "555-0012",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, 999, 1, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("Variant 999 not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithUnavailableVariant_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (product, _, unavailableVariant) = TestDataSeeder.SeedProductWithUnavailableVariant(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Unavailable Variant",
            CustomerPhone: "555-0013",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, unavailableVariant.Id, 1, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("not available", exception.Message);
        Assert.Contains(unavailableVariant.Name, exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithVariantFromWrongProduct_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product1, _, _, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (product2, availableVariant, _) = TestDataSeeder.SeedProductWithUnavailableVariant(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Wrong Product Variant",
            CustomerPhone: "555-0014",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product1.Id, availableVariant.Id, 1, null, [])
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("does not belong to product", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistentAddon_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Missing Addon",
            CustomerPhone: "555-0015",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(999, 1)
                    ]
                )
            ]
       );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("Addon 999 not found", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithUnavailableAddon_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var unavailableAddon = TestDataSeeder.SeedUnavailableAddon(context);
        // Link the unavailable addon to the product so it passes eligibility check
        TestDataSeeder.SeedProductAddons(context, product.Id, unavailableAddon.Id);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Unavailable Addon",
            CustomerPhone: "555-0016",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(unavailableAddon.Id, 1)
                    ]
                )
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("not available", exception.Message);
        Assert.Contains(unavailableAddon.Name, exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithZeroAddonQuantity_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, _, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Zero Addon Qty",
            CustomerPhone: "555-0017",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 0)
                    ]
                )
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("Addon quantity must be greater than 0", exception.Message);
    }

    #endregion

    #region CreateOrderAsync - Addon Eligibility Validation

    [Fact]
    public async Task CreateOrderAsync_WithIneligibleAddon_ThrowsException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        // Create addon but DON'T link it to the product
        var (extraShot, _, _) = TestDataSeeder.SeedAddons(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Ineligible Addon",
            CustomerPhone: "555-0030",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                )
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("not allowed for product", exception.Message);
        Assert.Contains(extraShot.Name, exception.Message);
        Assert.Contains(product.Name, exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_WithEligibleAddon_Succeeds()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, _, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Eligible Addon",
            CustomerPhone: "555-0031",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        Assert.True(response.OrderId > 0);
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Single(order.Items[0].Addons);
    }

    [Fact]
    public async Task CreateOrderAsync_DifferentProductsHaveDifferentAllowedAddons()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, coffeeProduct, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (_, pastryProduct) = TestDataSeeder.SeedSecondCategory(context);
        var pastryVariant = context.ProductVariants.First(v => v.ProductId == pastryProduct.Id);

        // Create addons
        var (extraShot, _, _) = TestDataSeeder.SeedAddons(context);

        // Link Extra Shot only to coffee, not to pastry
        TestDataSeeder.SeedProductAddons(context, coffeeProduct.Id, extraShot.Id);

        var service = new OrderService(context);

        // Coffee with Extra Shot should succeed
        var coffeeRequest = new CreateOrderRequest(
            CustomerName: "Coffee Order",
            CustomerPhone: "555-0032",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    coffeeProduct.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons: 
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                )
            ]
        );
        var coffeeResponse = await service.CreateOrderAsync(coffeeRequest);
        Assert.True(coffeeResponse.OrderId > 0);

        // Pastry with Extra Shot should fail
        var pastryRequest = new CreateOrderRequest(
            CustomerName: "Pastry Order",
            CustomerPhone: "555-0033",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    pastryProduct.Id,
                    pastryVariant.Id,
                    1,
                    null,
                    Addons: 
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ] // Should fail!
                )
            ]
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(pastryRequest));
        Assert.Contains("not allowed for product", exception.Message);
        Assert.Contains(pastryProduct.Name, exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_ProductWithNoAllowedAddons_RejectsAnyAddon()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        // Create addon but don't link to any product
        var (extraShot, _, _) = TestDataSeeder.SeedAddons(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "No Addons Allowed",
            CustomerPhone: "555-0034",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons: 
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                )
            ]
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));
        Assert.Contains("not allowed for product", exception.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_MultipleItemsWithDifferentAddonEligibility()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, coffeeProduct, smallVariant, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (_, pastryProduct) = TestDataSeeder.SeedSecondCategory(context);
        var pastryVariant = context.ProductVariants.First(v => v.ProductId == pastryProduct.Id);

        var (extraShot, oatMilk, _) = TestDataSeeder.SeedAddons(context);

        // Coffee gets Extra Shot, Pastry gets nothing
        TestDataSeeder.SeedProductAddons(context, coffeeProduct.Id, extraShot.Id, oatMilk.Id);

        var service = new OrderService(context);

        // Order with coffee (eligible addon) and pastry (no addon) should succeed
        var validRequest = new CreateOrderRequest(
            CustomerName: "Mixed Order Valid",
            CustomerPhone: "555-0035",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    coffeeProduct.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons: 
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                ),
                new CreateOrderItemRequest(
                    pastryProduct.Id,
                    pastryVariant.Id,
                    1,
                    null,
                    Addons: []
                )
            ]
        );
        var response = await service.CreateOrderAsync(validRequest);
        Assert.True(response.OrderId > 0);

        // Order with pastry trying to use coffee's addon should fail
        var invalidRequest = new CreateOrderRequest(
            CustomerName: "Mixed Order Invalid",
            CustomerPhone: "555-0036",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    coffeeProduct.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons: 
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                ),
                new CreateOrderItemRequest(
                    pastryProduct.Id,
                    pastryVariant.Id,
                    1,
                    null,
                    Addons: 
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ] // Should fail!
                )
            ]
        );

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(invalidRequest));
        Assert.Contains("not allowed for product", exception.Message);
        Assert.Contains(pastryProduct.Name, exception.Message);
    }

    #endregion

    #region GetOrderAsync

    [Fact]
    public async Task GetOrderAsync_WithValidId_ReturnsOrder()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var createRequest = new CreateOrderRequest(
            CustomerName: "Get Test",
            CustomerPhone: "555-0018",
            PickupTimeUtc: DateTime.UtcNow.AddHours(2),
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 1, "Test notes", [])
            ]
        );
        var createResponse = await service.CreateOrderAsync(createRequest);

        // Act
        var order = await service.GetOrderAsync(createResponse.OrderId);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(createResponse.OrderId, order.Id);
        Assert.Equal("Get Test", order.CustomerName);
        Assert.Single(order.Items);
        Assert.Equal("Test notes", order.Items[0].Notes);
    }

    [Fact]
    public async Task GetOrderAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new OrderService(context);

        // Act
        var order = await service.GetOrderAsync(999);

        // Assert
        Assert.Null(order);
    }

    [Fact]
    public async Task GetOrderAsync_IncludesAllItemsAndAddons()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, oatMilk, vanilla) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new OrderService(context);
        var createRequest = new CreateOrderRequest(
            CustomerName: "Full Order Test",
            CustomerPhone: "555-0019",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    smallVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                ),
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    2,
                    "With ice",
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(oatMilk.Id, 1),
                        new CreateOrderItemAddonRequest(vanilla.Id, 2)
                    ]
                )
            ]
        );
        var createResponse = await service.CreateOrderAsync(createRequest);

        // Act
        var order = await service.GetOrderAsync(createResponse.OrderId);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(2, order.Items.Count);

        var item1 = order.Items.First(i => i.VariantName == "Small");
        Assert.Single(item1.Addons);

        var item2 = order.Items.First(i => i.VariantName == "Medium");
        Assert.Equal(2, item2.Addons.Count);
    }

    #endregion

    #region Order Status and Payment Status

    [Fact]
    public async Task CreateOrderAsync_SetsInitialStatus_ToPending()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Status Test",
            CustomerPhone: "555-0020",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(Shared.Enums.OrderStatus.Pending, order.Status);
        Assert.Equal(Shared.Enums.PaymentStatus.Pending, order.PaymentStatus);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateOrderAsync_WithSameProductDifferentVariants_CreatesMultipleItems()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, mediumVariant, largeVariant) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Multi Variant",
            CustomerPhone: "555-0021",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, smallVariant.Id, 1, null, []),
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 1, null, []),
                new CreateOrderItemRequest(product.Id, largeVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(3, order.Items.Count);

        // Verify each variant is represented
        Assert.Contains(order.Items, i => i.VariantName == smallVariant.Name && i.BaseUnitPrice == smallVariant.Price);
        Assert.Contains(order.Items, i => i.VariantName == mediumVariant.Name && i.BaseUnitPrice == mediumVariant.Price);
        Assert.Contains(order.Items, i => i.VariantName == largeVariant.Name && i.BaseUnitPrice == largeVariant.Price);

        // Total = $3.50 + $4.50 + $5.50 = $13.50
        Assert.Equal(13.50m, order.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_WithSameAddonOnMultipleItems_WorksCorrectly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, smallVariant, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);
        var (extraShot, _, _) = TestDataSeeder.SeedAddonsForProduct(context, product.Id);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Same Addon Multi Item",
            CustomerPhone: "555-0022",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    smallVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 1)
                    ]
                ),
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(extraShot.Id, 2)
                    ]
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal(2, order.Items.Count);

        // Item 1: Small ($3.50) + 1 extra shot ($0.75) = $4.25
        // Item 2: Medium ($4.50) + 2 extra shots ($1.50) = $6.00
        // Total = $10.25
        Assert.Equal(10.25m, order.Total);
    }

    [Fact]
    public async Task CreateOrderAsync_PreservesItemNotes()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Notes Test",
            CustomerPhone: "555-0023",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    product.Id,
                    mediumVariant.Id,
                    1,
                    "Extra hot, no foam, extra caramel drizzle on top",
                    []
                )
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Equal("Extra hot, no foam, extra caramel drizzle on top", order.Items[0].Notes);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNullNotes_WorksCorrectly()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Null Notes",
            CustomerPhone: "555-0024",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.Null(order.Items[0].Notes);
    }

    [Fact]
    public async Task CreateOrderAsync_SetsCreatedAtUtc()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var (_, product, _, mediumVariant, _) = TestDataSeeder.SeedCategoryWithProductAndVariants(context);

        var beforeCreate = DateTime.UtcNow;

        var service = new OrderService(context);
        var request = new CreateOrderRequest(
            CustomerName: "Timestamp Test",
            CustomerPhone: "555-0025",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(product.Id, mediumVariant.Id, 1, null, [])
            ]
        );

        // Act
        var response = await service.CreateOrderAsync(request);
        var afterCreate = DateTime.UtcNow;

        // Assert
        var order = await service.GetOrderAsync(response.OrderId);
        Assert.NotNull(order);
        Assert.True(order.CreatedAtUtc >= beforeCreate);
        Assert.True(order.CreatedAtUtc <= afterCreate);
    }

    #endregion
}
