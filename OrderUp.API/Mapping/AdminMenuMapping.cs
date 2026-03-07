using OrderUp.API.Data.Entities;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Mapping;

/// <summary>
/// Mapping extensions for admin views (includes all items regardless of availability).
/// </summary>
public static class AdminMenuMapping
{
    /// <summary>
    /// Maps a Product to ProductDto for admin (includes all variants and all allowed addons).
    /// </summary>
    public static ProductDto ToAdminDto(this Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.ImageUrl,
            product.IsAvailable,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.Variants
                .Select(v => v.ToDto())
                .ToList(),
            product.ProductAddons
                .Select(pa => pa.ToAdminDto())
                .ToList()
        );
    }

    /// <summary>
    /// Maps ProductAddon to AddonDto for admin (includes regardless of availability).
    /// </summary>
    public static AddonDto ToAdminDto(this ProductAddon productAddon)
    {
        var addon = productAddon.Addon;
        return new AddonDto(
            addon.Id,
            addon.Name,
            addon.Price,
            addon.Group,
            productAddon.MaxPerItemOverride ?? addon.MaxPerItem,
            addon.IsAvailable
        );
    }
}
