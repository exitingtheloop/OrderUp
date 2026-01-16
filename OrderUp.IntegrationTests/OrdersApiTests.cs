using System.Net;
using System.Net.Http.Json;
using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;
using OrderUp.Shared.Enums;

namespace OrderUp.IntegrationTests;

public class OrdersApiTests : IDisposable
{
    private readonly OrderUpWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IntegrationTestSeeder.SeededData _seededData;

    public OrdersApiTests()
    {
        _factory = new OrderUpWebApplicationFactory();
        _client = _factory.CreateClient();

        // Seed test data
        using var context = _factory.GetDbContext();
        _seededData = IntegrationTestSeeder.SeedTestData(context);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    #region POST /api/orders - Valid Order Tests

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedWithOrderId()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "John Doe",
            CustomerPhone: "555-1234",
            PickupTimeUtc: DateTime.UtcNow.AddHours(1),
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.LatteProductId,
                    VariantId: _seededData.LatteLargeVariantId,
                    Quantity: 1,
                    Notes: null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(_seededData.ExtraShotAddonId, 2),
                        new CreateOrderItemAddonRequest(_seededData.OatMilkAddonId, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createResponse = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(createResponse);
        Assert.True(createResponse.OrderId > 0);
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_CanRetrieveOrderWithCorrectData()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Jane Doe",
            CustomerPhone: "555-5678",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.LatteProductId,
                    VariantId: _seededData.LatteLargeVariantId,
                    Quantity: 1,
                    Notes: "Extra hot",
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(_seededData.ExtraShotAddonId, 2),
                        new CreateOrderItemAddonRequest(_seededData.OatMilkAddonId, 1)
                    ]
                )
            ]
        );

        // Act - Create order
        var createResponse = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(created);

        // Act - Get order
        var getResponse = await _client.GetAsync($"/api/orders/{created.OrderId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Assert order details
        Assert.NotNull(order);
        Assert.Equal(created.OrderId, order.Id);
        Assert.Equal("Jane Doe", order.CustomerName);
        Assert.Equal("555-5678", order.CustomerPhone);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);

        // Assert item snapshots
        Assert.Single(order.Items);
        var item = order.Items[0];
        Assert.Equal("Latte", item.ProductName);
        Assert.Equal("Large", item.VariantName);
        Assert.Equal(150.00m, item.BaseUnitPrice);
        Assert.Equal(1, item.Quantity);
        Assert.Equal("Extra hot", item.Notes);

        // Assert addon snapshots
        Assert.Equal(2, item.Addons.Count);

        var extraShotAddon = item.Addons.FirstOrDefault(a => a.Name == "Extra Shot");
        Assert.NotNull(extraShotAddon);
        Assert.Equal(30.00m, extraShotAddon.UnitPrice);
        Assert.Equal(2, extraShotAddon.Quantity);
        Assert.Equal("Espresso", extraShotAddon.Group);

        var oatMilkAddon = item.Addons.FirstOrDefault(a => a.Name == "Oat Milk");
        Assert.NotNull(oatMilkAddon);
        Assert.Equal(20.00m, oatMilkAddon.UnitPrice);
        Assert.Equal(1, oatMilkAddon.Quantity);
        Assert.Equal("Milk", oatMilkAddon.Group);

        // Assert total: 150 + (2 * 30) + (1 * 20) = 150 + 60 + 20 = 230
        Assert.Equal(230.00m, order.Total);
    }

    [Fact]
    public async Task CreateOrder_WithNoAddons_Succeeds()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "No Addon Order",
            CustomerPhone: "555-0000",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.CroissantProductId,
                    VariantId: _seededData.CroissantDefaultVariantId,
                    Quantity: 2,
                    Notes: null,
                    Addons: []
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(created);

        var order = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{created.OrderId}");
        Assert.NotNull(order);
        Assert.Equal(160.00m, order.Total); // 80 * 2
    }

    #endregion

    #region POST /api/orders - Validation Failure Tests

    [Fact]
    public async Task CreateOrder_WithEmptyItems_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Empty Order",
            CustomerPhone: "555-0000",
            PickupTimeUtc: null,
            Items: []
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_WithAddonNotAllowedForProduct_ReturnsBadRequest()
    {
        // Arrange - Croissant does not allow Extra Shot
        var request = new CreateOrderRequest(
            CustomerName: "Invalid Addon",
            CustomerPhone: "555-1111",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.CroissantProductId,
                    VariantId: _seededData.CroissantDefaultVariantId,
                    Quantity: 1,
                    Notes: null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(_seededData.ExtraShotAddonId, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("not allowed for product", errorBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithUnavailableAddon_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Unavailable Addon",
            CustomerPhone: "555-6666",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.LatteProductId,
                    VariantId: _seededData.LatteLargeVariantId,
                    Quantity: 1,
                    Notes: null,
                    Addons:
                    [
                        new CreateOrderItemAddonRequest(_seededData.UnavailableAddonId, 1)
                    ]
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("not available", errorBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithMismatchedVariant_ReturnsBadRequest()
    {
        // Arrange - Use Latte ProductId with Croissant VariantId
        var request = new CreateOrderRequest(
            CustomerName: "Mismatched Variant",
            CustomerPhone: "555-2222",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.LatteProductId,
                    VariantId: _seededData.CroissantDefaultVariantId, // Wrong variant!
                    Quantity: 1,
                    Notes: null,
                    Addons: []
                    )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("does not belong to product", errorBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithNonExistentProduct_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Non-existent Product",
            CustomerPhone: "555-3333",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: 99999,
                    VariantId: _seededData.LatteLargeVariantId,
                    Quantity: 1,
                    Notes: null,
                    Addons: []
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", errorBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithUnavailableProduct_ReturnsBadRequest()
    {
        // Arrange - Use the unavailable product
        var request = new CreateOrderRequest(
            CustomerName: "Unavailable Product",
            CustomerPhone: "555-4444",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.UnavailableProductId,
                    VariantId: _seededData.UnavailableProductVariantId,
                    Quantity: 1,
                    Notes: null,
                    Addons: []
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("not available", errorBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOrder_WithZeroQuantity_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Zero Quantity",
            CustomerPhone: "555-5555",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(
                    ProductId: _seededData.LatteProductId,
                    VariantId: _seededData.LatteLargeVariantId,
                    Quantity: 0,
                    Notes: null,
                    Addons: []
                )
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var errorBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("quantity", errorBody, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region POST /api/orders - Multi-Item Tests

    [Fact]
    public async Task CreateOrder_WithMultipleItems_CalculatesTotalCorrectly()
    {
        // Arrange - Latte Large (150) + Croissant (80) = 230
        var request = new CreateOrderRequest(
            CustomerName: "Multi Item",
            CustomerPhone: "555-7777",
            PickupTimeUtc: null,
            Items:
            [
                new CreateOrderItemRequest(_seededData.LatteProductId, _seededData.LatteLargeVariantId, 1, null, []),
                new CreateOrderItemRequest(_seededData.CroissantProductId, _seededData.CroissantDefaultVariantId, 1, null, [])
            ]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(created);

        var order = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{created.OrderId}");

        // Assert
        Assert.NotNull(order);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(230.00m, order.Total);
    }

    #endregion

    #region GET /api/orders/{id} Tests

    [Fact]
    public async Task GetOrder_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/orders/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
