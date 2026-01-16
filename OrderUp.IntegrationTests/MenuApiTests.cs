using System.Net;
using System.Net.Http.Json;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.IntegrationTests;

public class MenuApiTests : IDisposable
{
    private readonly OrderUpWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly IntegrationTestSeeder.SeededData _seededData;

    public MenuApiTests()
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

    #region GET /api/menu Tests

    [Fact]
    public async Task GetMenu_ReturnsSuccess_WithCorrectStructure()
    {
        // Act
        var response = await _client.GetAsync("/api/menu");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var menu = await response.Content.ReadFromJsonAsync<MenuDto>();
        Assert.NotNull(menu);
        Assert.NotNull(menu.Categories);
        Assert.NotNull(menu.Products);
    }

    [Fact]
    public async Task GetMenu_ReturnsLatteWithVariantsAndAllowedAddons()
    {
        // Act
        var menu = await _client.GetFromJsonAsync<MenuDto>("/api/menu");

        // Assert
        Assert.NotNull(menu);

        var latte = menu.Products.FirstOrDefault(p => p.Name == "Latte");
        Assert.NotNull(latte);
        Assert.True(latte.IsAvailable);

        // Variants
        Assert.Equal(2, latte.Variants.Count);
        Assert.Contains(latte.Variants, v => v.Name == "Small" && v.Price == 100.00m);
        Assert.Contains(latte.Variants, v => v.Name == "Large" && v.Price == 150.00m);

        // AllowedAddons - Latte should have Extra Shot and Oat Milk
        Assert.Equal(2, latte.AllowedAddons.Count);
        Assert.Contains(latte.AllowedAddons, a => a.Name == "Extra Shot" && a.Price == 30.00m);
        Assert.Contains(latte.AllowedAddons, a => a.Name == "Oat Milk" && a.Price == 20.00m);
    }

    [Fact]
    public async Task GetMenu_ReturnsCroissantWithEmptyAllowedAddons()
    {
        // Act
        var menu = await _client.GetFromJsonAsync<MenuDto>("/api/menu");

        // Assert
        Assert.NotNull(menu);

        var croissant = menu.Products.FirstOrDefault(p => p.Name == "Croissant");
        Assert.NotNull(croissant);
        Assert.True(croissant.IsAvailable);

        // Croissant has no allowed addons
        Assert.Empty(croissant.AllowedAddons);

        // Has its variant
        Assert.Single(croissant.Variants);
        Assert.Equal("Default", croissant.Variants[0].Name);
        Assert.Equal(80.00m, croissant.Variants[0].Price);
    }

    [Fact]
    public async Task GetMenu_ExcludesUnavailableProducts()
    {
        // Act
        var menu = await _client.GetFromJsonAsync<MenuDto>("/api/menu");

        // Assert
        Assert.NotNull(menu);
        Assert.DoesNotContain(menu.Products, p => p.Name == "Seasonal Special");
    }

    [Fact]
    public async Task GetMenu_ExcludesUnavailableAddonsFromAllowedAddons()
    {
        // Act
        var menu = await _client.GetFromJsonAsync<MenuDto>("/api/menu");

        // Assert
        Assert.NotNull(menu);

        // No product should have the unavailable "Seasonal Syrup" addon
        foreach (var product in menu.Products)
        {
            Assert.DoesNotContain(product.AllowedAddons, a => a.Name == "Seasonal Syrup");
        }
    }

    [Fact]
    public async Task GetMenu_ReturnsCategories()
    {
        // Act
        var menu = await _client.GetFromJsonAsync<MenuDto>("/api/menu");

        // Assert
        Assert.NotNull(menu);
        Assert.Contains(menu.Categories, c => c.Name == "Coffee");
        Assert.Contains(menu.Categories, c => c.Name == "Pastries");
    }

    #endregion
}
