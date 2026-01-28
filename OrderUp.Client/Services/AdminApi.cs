using System.Net.Http.Json;
using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Shared.Contracts.Menu.Responses;
using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;

namespace OrderUp.Client.Services;

/// <summary>
/// Client for admin API endpoints.
/// </summary>
public class AdminApi
{
    private readonly HttpClient _httpClient;

    public AdminApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region Orders

    /// <summary>
    /// Gets all orders created today.
    /// </summary>
    public async Task<List<OrderDto>> GetTodaysOrdersAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<OrderDto>>("api/admin/orders/today");
        return result ?? [];
    }

    /// <summary>
    /// Updates the status of an order.
    /// </summary>
    public async Task<OrderDto?> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/admin/orders/{orderId}/status", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>();
    }

    #endregion

    #region Products

    /// <summary>
    /// Gets all products (including unavailable).
    /// </summary>
    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<ProductDto>>("api/admin/products");
        return result ?? [];
    }

    /// <summary>
    /// Gets a single product with details.
    /// </summary>
    public async Task<ProductDto?> GetProductAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<ProductDto>($"api/admin/products/{id}");
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/products", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/products/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    /// <summary>
    /// Deletes a product.
    /// </summary>
    public async Task<bool> DeleteProductAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/products/{id}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Updates the allowed addons for a product.
    /// </summary>
    public async Task<ProductDto?> UpdateProductAddonsAsync(int productId, UpdateProductAddonsRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/products/{productId}/addons", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    #endregion

    #region Variants

    /// <summary>
    /// Creates a new variant for a product.
    /// </summary>
    public async Task<ProductVariantDto?> CreateVariantAsync(int productId, CreateProductVariantRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/admin/products/{productId}/variants", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductVariantDto>();
    }

    /// <summary>
    /// Updates a variant.
    /// </summary>
    public async Task<ProductVariantDto?> UpdateVariantAsync(int productId, int variantId, UpdateProductVariantRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/products/{productId}/variants/{variantId}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductVariantDto>();
    }

    /// <summary>
    /// Deletes a variant.
    /// </summary>
    public async Task<bool> DeleteVariantAsync(int productId, int variantId)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/products/{productId}/variants/{variantId}");
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Addons

    /// <summary>
    /// Gets all addons.
    /// </summary>
    public async Task<List<AddonDto>> GetAllAddonsAsync()
    {
        var result = await _httpClient.GetFromJsonAsync<List<AddonDto>>("api/admin/addons");
        return result ?? [];
    }

    /// <summary>
    /// Gets a single addon.
    /// </summary>
    public async Task<AddonDto?> GetAddonAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<AddonDto>($"api/admin/addons/{id}");
    }

    /// <summary>
    /// Creates a new addon.
    /// </summary>
    public async Task<AddonDto?> CreateAddonAsync(CreateAddonRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/admin/addons", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AddonDto>();
    }

    /// <summary>
    /// Updates an existing addon.
    /// </summary>
    public async Task<AddonDto?> UpdateAddonAsync(int id, UpdateAddonRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/admin/addons/{id}", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AddonDto>();
    }

    /// <summary>
    /// Deletes an addon.
    /// </summary>
    public async Task<bool> DeleteAddonAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/admin/addons/{id}");
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Categories

    /// <summary>
    /// Gets all categories (from the public menu endpoint).
    /// </summary>
    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        var menu = await _httpClient.GetFromJsonAsync<MenuDto>("api/menu");
        return menu?.Categories ?? [];
    }

    #endregion
}
