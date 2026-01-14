using OrderUp.API.Data.Entities;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Mapping;

public static class MenuMapping
{
    public static CategoryDto ToDto(this ProductCategory category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.DisplayOrder
        );
    }

    public static ProductVariantDto ToDto(this ProductVariant variant)
    {
        return new ProductVariantDto(
            variant.Id,
            variant.Name,
            variant.Price,
            variant.IsDefault,
            variant.IsAvailable
        );
    }

    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.ImageUrl,
            product.IsAvailable,
            product.CategoryId,
            product.Category.Name,
            product.Variants
                .Select(v => v.ToDto())
                .ToList(),
            product.ProductAddons
                .Where(pa => pa.Addon.IsAvailable)
                .Select(pa => pa.ToDto())
                .ToList()
      );
    }

    public static AddonDto ToDto(this Addon addon)
    {
        return new AddonDto(
            addon.Id,
            addon.Name,
            addon.Price,
            addon.Group,
            addon.MaxPerItem,
            addon.IsAvailable
        );
    }

    public static AddonDto ToDto(this ProductAddon productAddon)
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
