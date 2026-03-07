using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.Shared.Contracts.Payments.Requests;

namespace OrderUp.IntegrationTests;

/// <summary>
/// Integration tests for payment API endpoints.
/// Note: These tests use the real gateway implementations but without valid API keys,
/// so they test the controller/service layer but not actual payment provider integration.
/// For full end-to-end testing, use manual testing with test API keys.
/// </summary>
public class PaymentsApiTests : IDisposable
{
    private readonly OrderUpWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentsApiTests()
    {
        _factory = new OrderUpWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsBadRequest_ForNonExistentOrder()
    {
        // Arrange
        var request = new CreateCheckoutSessionRequest(99999);

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments/checkout", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsBadRequest_ForAlreadyPaidOrder()
    {
        // Arrange
        var orderId = await SeedTestOrderAsync(PaymentStatus.Paid);
        var request = new CreateCheckoutSessionRequest(orderId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments/checkout", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("already paid", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCheckoutSession_ReturnsBadRequest_ForOrderWithNoItems()
    {
        // Arrange - Create order with no items
        int orderId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var order = new Order
            {
                CustomerName = "Test",
                CustomerPhone = "123",
                PaymentStatus = PaymentStatus.Pending,
                Items = new List<OrderItem>() // Empty!
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            orderId = order.Id;
        }

        var request = new CreateCheckoutSessionRequest(orderId);

        // Act
        var response = await _client.PostAsJsonAsync("/api/payments/checkout", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("no items", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Webhook_ReturnsBadRequest_ForUnknownProvider()
    {
        // Act
        var response = await _client.PostAsync("/api/payments/webhook/unknownprovider",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Unknown payment provider", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Webhook_Stripe_ReturnsBadRequest_ForMissingSignature()
    {
        // Act - Call Stripe webhook without signature header
        var response = await _client.PostAsync("/api/payments/webhook/stripe",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert - Should fail signature verification
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_PayMongo_ReturnsBadRequest_ForMissingSignature()
    {
        // Act - Call PayMongo webhook without signature header
        var response = await _client.PostAsync("/api/payments/webhook/paymongo",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert - Should fail signature verification
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<int> SeedTestOrderAsync(PaymentStatus status = PaymentStatus.Pending)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        // Create category
        var category = new ProductCategory { Name = $"Test_{Guid.NewGuid()}", DisplayOrder = 1 };
        context.ProductCategories.Add(category);
        await context.SaveChangesAsync();

        // Create product with variant
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

        // Create order
        var order = new Order
        {
            CustomerName = "Test Customer",
            CustomerPhone = "09171234567",
            PaymentStatus = status,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    Quantity = 1,
                    BaseUnitPriceSnapshot = 100.00m,
                    ProductNameSnapshot = "Test Product",
                    VariantNameSnapshot = "Regular"
                }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return order.Id;
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
