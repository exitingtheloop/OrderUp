using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.API.Mapping;
using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Services.Admin;

public class AdminMenuService : IAdminMenuService
{
    private readonly DataContext _context;

    public AdminMenuService(DataContext context)
    {
        _context = context;
    }

    #region Products

    public async Task<List<ProductDto>> GetAllProductsAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.ProductAddons)
            .ThenInclude(pa => pa.Addon)
            .OrderBy(p => p.Category.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return products.Select(p => p.ToAdminDto()).ToList();
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var product = await _context.Products
        .AsNoTracking()
        .Include(p => p.Category)
        .Include(p => p.Variants)
        .Include(p => p.ProductAddons)
        .ThenInclude(pa => pa.Addon)
        .FirstOrDefaultAsync(p => p.Id == id);

        return product?.ToAdminDto();
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductRequest request)
    {
        // Validate category exists
        var categoryExists = await _context.ProductCategories.AnyAsync(c => c.Id == request.CategoryId);

        if (!categoryExists)
            throw new InvalidOperationException($"Category {request.CategoryId} not found.");

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsAvailable = request.IsAvailable
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Reload with includes
        return (await GetProductAsync(product.Id))!;
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.ProductAddons)
            .ThenInclude(pa => pa.Addon)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return null;

        // Validate category exists if changing
        if (request.CategoryId != product.CategoryId)
        {
            var categoryExists = await _context.ProductCategories.AnyAsync(c => c.Id == request.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"Category {request.CategoryId} not found.");
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.ImageUrl = request.ImageUrl;
        product.IsAvailable = request.IsAvailable;
        product.CategoryId = request.CategoryId;

        await _context.SaveChangesAsync();

        return product.ToAdminDto();
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product is null)
            return false;

        // Check for order references (Restrict delete behavior will prevent this anyway)
        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
        if (hasOrders)
            throw new InvalidOperationException($"Cannot delete product '{product.Name}' because it has associated orders.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Variants

    public async Task<ProductVariantDto?> GetVariantAsync(int productId, int variantId)
    {
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ProductId == productId && v.Id == variantId);

        return variant?.ToDto();
    }

    public async Task<ProductVariantDto> CreateVariantAsync(int productId, CreateProductVariantRequest request)
    {
        // Validate product exists
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);

        if (!productExists)
            throw new InvalidOperationException($"Product {productId} not found.");

        var variant = new ProductVariant
        {
            ProductId = productId,
            Name = request.Name,
            Price = request.Price,
            IsDefault = request.IsDefault,
            IsAvailable = request.IsAvailable
        };

        // If this is set as default, unset other defaults
        if (request.IsDefault)
        {
            await UnsetOtherDefaults(productId, null);
        }

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        return variant.ToDto();
    }

    public async Task<ProductVariantDto?> UpdateVariantAsync(int productId, int variantId, UpdateProductVariantRequest request)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.ProductId == productId && v.Id == variantId);

        if (variant is null)
            return null;

        // If setting as default, unset other defaults
        if (request.IsDefault && !variant.IsDefault)
        {
            await UnsetOtherDefaults(productId, variantId);
        }

        variant.Name = request.Name;
        variant.Price = request.Price;
        variant.IsDefault = request.IsDefault;
        variant.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();

        return variant.ToDto();
    }

    public async Task<bool> DeleteVariantAsync(int productId, int variantId)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.ProductId == productId && v.Id == variantId);

        if (variant is null)
            return false;

        // Check for order references
        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.VariantId == variantId);
        if (hasOrders)
            throw new InvalidOperationException($"Cannot delete variant '{variant.Name}' because it has associated orders.");

        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task UnsetOtherDefaults(int productId, int? excludeVariantId)
    {
        var otherDefaults = await _context.ProductVariants
            .Where(v => v.ProductId == productId && v.IsDefault && v.Id != excludeVariantId)
            .ToListAsync();

        foreach (var v in otherDefaults)
        {
            v.IsDefault = false;
        }
    }

    #endregion

    #region Product Addons Mapping

    public async Task<ProductDto?> UpdateProductAddonsAsync(int productId, UpdateProductAddonsRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .Include(p => p.ProductAddons)
            .ThenInclude(pa => pa.Addon)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product is null)
            return null;

        // Validate all addon IDs exist
        if (request.AllowedAddonIds.Count > 0)
        {
            var existingAddonIds = await _context.Addons
              .Where(a => request.AllowedAddonIds.Contains(a.Id))
              .Select(a => a.Id)
                 .ToListAsync();

            var missingIds = request.AllowedAddonIds.Except(existingAddonIds).ToList();
            if (missingIds.Count > 0)
                throw new InvalidOperationException($"Addon(s) not found: {string.Join(", ", missingIds)}");
        }

        // Remove existing mappings
        _context.ProductAddons.RemoveRange(product.ProductAddons);

        // Add new mappings
        foreach (var addonId in request.AllowedAddonIds.Distinct())
        {
            _context.ProductAddons.Add(new ProductAddon
            {
                ProductId = productId,
                AddonId = addonId
            });
        }

        await _context.SaveChangesAsync();

        // Reload with updated addons
        return (await GetProductAsync(productId))!;
    }

    #endregion
}
