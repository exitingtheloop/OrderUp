using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.API.Payments;
using OrderUp.API.Payments.Abstractions;
using OrderUp.Tests.Helpers;

namespace OrderUp.Tests.Payments;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentGatewayResolver> _resolverMock;
    private readonly Mock<IPaymentGateway> _gatewayMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;

    public PaymentServiceTests()
    {
        _gatewayMock = new Mock<IPaymentGateway>();
        _gatewayMock.Setup(g => g.Name).Returns("TestGateway");

        _resolverMock = new Mock<IPaymentGatewayResolver>();
        _resolverMock.Setup(r => r.GetActiveGateway()).Returns(_gatewayMock.Object);
        _resolverMock.Setup(r => r.GetGateway(It.IsAny<string>())).Returns(_gatewayMock.Object);

        _loggerMock = new Mock<ILogger<PaymentService>>();
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ReturnsCheckoutUrl_WhenOrderIsValid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var order = await CreateTestOrderAsync(context);

        _gatewayMock
            .Setup(g => g.CreateCheckoutAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://checkout.test/session123", "session123"));

        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);

        // Act
        var checkoutUrl = await service.CreateCheckoutSessionAsync(order.Id);

        // Assert
        Assert.Equal("https://checkout.test/session123", checkoutUrl);

        // Verify order was updated
        var updatedOrder = await context.Orders.FindAsync(order.Id);
        Assert.Equal("TestGateway", updatedOrder!.PaymentProvider);
        Assert.Equal("session123", updatedOrder.PaymentSessionId);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ThrowsException_WhenOrderNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateCheckoutSessionAsync(999));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ThrowsException_WhenOrderAlreadyPaid()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var order = await CreateTestOrderAsync(context, PaymentStatus.Paid);

        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateCheckoutSessionAsync(order.Id));
        Assert.Contains("already paid", ex.Message);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ThrowsException_WhenOrderHasNoItems()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var order = new Order
        {
            CustomerName = "Test",
            CustomerPhone = "1234567890",
            Items = new List<OrderItem>() // Empty!
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateCheckoutSessionAsync(order.Id));
        Assert.Contains("no items", ex.Message);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_RecreatesSession_WhenSessionAlreadyExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var order = await CreateTestOrderAsync(context);
        order.PaymentProvider = "TestGateway";
        order.PaymentSessionId = "old_session";
        await context.SaveChangesAsync();

        _gatewayMock
            .Setup(g => g.CreateCheckoutAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("https://checkout.test/new_session", "new_session"));

        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);

        // Act - Should not throw, should recreate
        var checkoutUrl = await service.CreateCheckoutSessionAsync(order.Id);

        // Assert
        Assert.Equal("https://checkout.test/new_session", checkoutUrl);
        var updatedOrder = await context.Orders.FindAsync(order.Id);
        Assert.Equal("new_session", updatedOrder!.PaymentSessionId);
    }

    [Fact]
    public async Task HandleWebhookAsync_ThrowsException_WhenProviderUnknown()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        _resolverMock.Setup(r => r.GetGateway("unknown")).Returns((IPaymentGateway?)null);

        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);
        var mockRequest = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.HandleWebhookAsync("unknown", mockRequest.Object));
        Assert.Contains("Unknown payment provider", ex.Message);
    }

    [Fact]
    public async Task HandleWebhookAsync_DelegatesToCorrectGateway()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var mockRequest = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();

        var service = new PaymentService(_resolverMock.Object, context, _loggerMock.Object);

        // Act
        await service.HandleWebhookAsync("TestGateway", mockRequest.Object);

        // Assert
        _gatewayMock.Verify(
            g => g.HandleWebhookAsync(mockRequest.Object, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static async Task<Order> CreateTestOrderAsync(
        DataContext context,
        PaymentStatus paymentStatus = PaymentStatus.Pending)
    {
        // Create minimal product structure for the order
        var category = new ProductCategory { Name = "Test Category", DisplayOrder = 1 };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            CategoryId = category.Id,
            Name = "Test Product",
            IsAvailable = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var variant = new ProductVariant
        {
            ProductId = product.Id,
            Name = "Regular",
            Price = 100.00m,
            IsDefault = true,
            IsAvailable = true
        };
        context.ProductVariants.Add(variant);
        await context.SaveChangesAsync();

        var order = new Order
        {
            CustomerName = "Test Customer",
            CustomerPhone = "09171234567",
            PaymentStatus = paymentStatus,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    Quantity = 1,
                    BaseUnitPriceSnapshot = 100.00m,
                    ProductNameSnapshot = "Test Product",
                    VariantNameSnapshot = "Regular",
                    Addons = new List<OrderItemAddon>()
                }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return order;
    }
}
