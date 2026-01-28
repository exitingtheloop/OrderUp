using System.Net;
using System.Net.Http.Json;
using OrderUp.API.Data.Seed;
using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.IntegrationTests;

public class AdminProductsApiTests : IDisposable
{
    private readonly OrderUpWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IntegrationTestSeeder.SeededData _seededData;

    public AdminProductsApiTests()
    {
        _factory = new OrderUpWebApplicationFactory(IdentitySeeder.AdminRole);
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

    #region GET /api/admin/products

    [Fact]
    public async Task GetAllProducts_AsAdmin_ReturnsAllProductsIncludingUnavailable()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/products");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.Equal(3, products.Count); // Latte, Croissant, UnavailableProduct
        Assert.Contains(products, p => p.Name == "Seasonal Special" && !p.IsAvailable);
    }

    #endregion

    #region GET /api/admin/products/{id}

    [Fact]
    public async Task GetProduct_AsAdmin_ReturnsProductWithDetails()
    {
        // Act
        var response = await _client.GetAsync($"/api/admin/products/{_seededData.LatteProductId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal("Latte", product.Name);
        Assert.Equal(2, product.Variants.Count);
        Assert.Equal(3, product.AllowedAddons.Count);
    }

    [Fact]
    public async Task GetProduct_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/products/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/admin/products

    [Fact]
    public async Task CreateProduct_AsAdmin_ReturnsCreated()
    {
        // Arrange
        var request = new CreateProductRequest(
            CategoryId: _seededData.CoffeeCategoryId,
            Name: "Espresso",
            Description: "Strong coffee",
            ImageUrl: null,
            IsAvailable: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal("Espresso", product.Name);
        Assert.Equal("Strong coffee", product.Description);
    }

    [Fact]
    public async Task CreateProduct_InvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateProductRequest(
            CategoryId: 99999,
            Name: "Test",
            Description: null,
            ImageUrl: null,
            IsAvailable: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/products", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PUT /api/admin/products/{id}

    [Fact]
    public async Task UpdateProduct_AsAdmin_ReturnsUpdatedProduct()
    {
        // Arrange
        var request = new UpdateProductRequest(
            Name: "Updated Latte",
            Description: "Updated description",
            ImageUrl: "new.jpg",
            IsAvailable: false,
            CategoryId: _seededData.CoffeeCategoryId
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/products/{_seededData.LatteProductId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Equal("Updated Latte", product.Name);
        Assert.False(product.IsAvailable);
    }

    #endregion

    #region DELETE /api/admin/products/{id}

    [Fact]
    public async Task DeleteProduct_AsAdmin_ReturnsNoContent()
    {
        // Create a product without orders to delete
        var createResponse = await _client.PostAsJsonAsync("/api/admin/products",
            new CreateProductRequest(_seededData.CoffeeCategoryId, "ToDelete", null, null, true));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/admin/products/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion

    #region Variants CRUD

    [Fact]
    public async Task CreateVariant_AsAdmin_ReturnsCreated()
    {
        // Arrange
        var request = new CreateProductVariantRequest(
            Name: "Extra Large",
            Price: 6.50m,
            IsDefault: false,
            IsAvailable: true
        );

        // Act
        var response = await _client.PostAsJsonAsync(
                  $"/api/admin/products/{_seededData.LatteProductId}/variants", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var variant = await response.Content.ReadFromJsonAsync<ProductVariantDto>();
        Assert.NotNull(variant);
        Assert.Equal("Extra Large", variant.Name);
        Assert.Equal(6.50m, variant.Price);
    }

    [Fact]
    public async Task UpdateVariant_AsAdmin_ReturnsUpdatedVariant()
    {
        // Arrange
        var request = new UpdateProductVariantRequest(
            Name: "Tiny",
            Price: 2.00m,
            IsDefault: false,
            IsAvailable: false
        );

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/admin/products/{_seededData.LatteProductId}/variants/" +
            $"{_seededData.LatteSmallVariantId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var variant = await response.Content.ReadFromJsonAsync<ProductVariantDto>();
        Assert.NotNull(variant);
        Assert.Equal("Tiny", variant.Name);
        Assert.Equal(2.00m, variant.Price);
    }

    #endregion

    #region Product Addons Mapping

    [Fact]
    public async Task UpdateProductAddons_AsAdmin_ReplacesAddons()
    {
        // Arrange - Set only one addon
        var request = new UpdateProductAddonsRequest([_seededData.ExtraShotAddonId]);

        // Act
        var response = await _client.PutAsJsonAsync(
          $"/api/admin/products/{_seededData.LatteProductId}/addons", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Single(product.AllowedAddons);
        Assert.Equal("Extra Shot", product.AllowedAddons[0].Name);
    }

    [Fact]
    public async Task UpdateProductAddons_ClearAll_ReturnsEmptyAddons()
    {
        // Arrange
        var request = new UpdateProductAddonsRequest([]);

        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/admin/products/{_seededData.LatteProductId}/addons", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.NotNull(product);
        Assert.Empty(product.AllowedAddons);
    }

    #endregion
}

public class AdminAddonsApiTests : IDisposable
{
    private readonly OrderUpWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IntegrationTestSeeder.SeededData _seededData;

    public AdminAddonsApiTests()
    {
        _factory = new OrderUpWebApplicationFactory(IdentitySeeder.AdminRole);
        _client = _factory.CreateClient();

        using var context = _factory.GetDbContext();
        _seededData = IntegrationTestSeeder.SeedTestData(context);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    #region GET /api/admin/addons

    [Fact]
    public async Task GetAllAddons_AsAdmin_ReturnsAllAddons()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/addons");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var addons = await response.Content.ReadFromJsonAsync<List<AddonDto>>();
        Assert.NotNull(addons);
        Assert.Equal(3, addons.Count);
    }

    #endregion

    #region POST /api/admin/addons

    [Fact]
    public async Task CreateAddon_AsAdmin_ReturnsCreated()
    {
        // Arrange
        var request = new CreateAddonRequest(
            Name: "Hazelnut Syrup",
            Price: 0.50m,
            Group: "Syrups",
            MaxPerItem: 2,
            IsAvailable: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/admin/addons", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var addon = await response.Content.ReadFromJsonAsync<AddonDto>();
        Assert.NotNull(addon);
        Assert.Equal("Hazelnut Syrup", addon.Name);
        Assert.Equal(0.50m, addon.Price);
    }

    #endregion

    #region PUT /api/admin/addons/{id}

    [Fact]
    public async Task UpdateAddon_AsAdmin_ReturnsUpdatedAddon()
    {
        // Arrange
        var request = new UpdateAddonRequest(
            Name: "Double Shot",
            Price: 1.00m,
            Group: "Espresso",
            MaxPerItem: 5,
            IsAvailable: false
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/admin/addons/{_seededData.ExtraShotAddonId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var addon = await response.Content.ReadFromJsonAsync<AddonDto>();
        Assert.NotNull(addon);
        Assert.Equal("Double Shot", addon.Name);
        Assert.Equal(1.00m, addon.Price);
        Assert.False(addon.IsAvailable);
    }

    #endregion

    #region DELETE /api/admin/addons/{id}

    [Fact]
    public async Task DeleteAddon_AsAdmin_ReturnsNoContent()
    {
        // Create an addon without orders to delete
        var createResponse = await _client.PostAsJsonAsync("/api/admin/addons",
            new CreateAddonRequest("ToDelete", 0.25m, "Test", null, true));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AddonDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/admin/addons/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    #endregion
}

public class AdminAuthorizationTests : IDisposable
{
    private readonly OrderUpWebApplicationFactory _factoryNoAuth;
    private readonly OrderUpWebApplicationFactory _factoryWrongRole;

    public AdminAuthorizationTests()
    {
        _factoryNoAuth = new OrderUpWebApplicationFactory(); // No test auth
        _factoryWrongRole = new OrderUpWebApplicationFactory("Customer"); // Wrong role
    }

    public void Dispose()
    {
        _factoryNoAuth.Dispose();
        _factoryWrongRole.Dispose();
    }

    [Fact]
    public async Task AdminEndpoint_WithoutAuth_Returns401()
    {
        // Arrange
        var client = _factoryNoAuth.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/products");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithWrongRole_Returns403()
    {
        // Arrange
        var client = _factoryWrongRole.CreateClient();

        // Act
        var response = await client.GetAsync("/api/admin/products");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
