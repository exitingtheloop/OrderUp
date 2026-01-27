using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Services.Admin;

public interface IAdminMenuService
{
    // Products
    Task<List<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductAsync(int id);
    Task<ProductDto> CreateProductAsync(CreateProductRequest request);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(int id);

    // Product Variants
    Task<ProductVariantDto?> GetVariantAsync(int productId, int variantId);
    Task<ProductVariantDto> CreateVariantAsync(int productId, CreateProductVariantRequest request);
    Task<ProductVariantDto?> UpdateVariantAsync(int productId, int variantId, UpdateProductVariantRequest request);
    Task<bool> DeleteVariantAsync(int productId, int variantId);

    // Product Addons (allowed addons mapping)
    Task<ProductDto?> UpdateProductAddonsAsync(int productId, UpdateProductAddonsRequest request);
}
