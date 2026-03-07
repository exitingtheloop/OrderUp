namespace OrderUp.API.Data.Entities;

/// <summary>
/// Junction table for product-addon eligibility.
/// Determines which addons can be applied to which products.
/// </summary>
public class ProductAddon
{
    public int ProductId { get; set; }
    public int AddonId { get; set; }

    /// <summary>
    /// Optional override for MaxPerItem specific to this product-addon combination.
    /// If null, falls back to Addon.MaxPerItem.
    /// </summary>
    public int? MaxPerItemOverride { get; set; }

    public Product Product { get; set; } = null!;
    public Addon Addon { get; set; } = null!;
}
