using OrderUp.API.Data;
using OrderUp.API.Data.Entities;

namespace OrderUp.IntegrationTests;

/// <summary>
/// Seeds minimal test data for integration tests.
/// Provides IDs for use in test assertions.
/// </summary>
public static class IntegrationTestSeeder
{
    public class SeededData
    {
        // Categories
        public int CoffeeCategoryId { get; set; }
   public int PastriesCategoryId { get; set; }

    // Products
 public int LatteProductId { get; set; }
  public int CroissantProductId { get; set; }
        public int UnavailableProductId { get; set; }

        // Variants
        public int LatteSmallVariantId { get; set; }
        public int LatteLargeVariantId { get; set; }
     public int CroissantDefaultVariantId { get; set; }
public int UnavailableProductVariantId { get; set; }

     // Addons
        public int ExtraShotAddonId { get; set; }
 public int OatMilkAddonId { get; set; }
        public int UnavailableAddonId { get; set; }
    }

    /// <summary>
    /// Seeds minimal menu data for integration testing.
    /// Returns IDs of seeded entities for use in test requests.
    /// </summary>
    public static SeededData SeedTestData(DataContext context)
    {
        var data = new SeededData();

    // Categories
        var coffeeCategory = new ProductCategory { Name = "Coffee", DisplayOrder = 1 };
        var pastriesCategory = new ProductCategory { Name = "Pastries", DisplayOrder = 2 };
        context.ProductCategories.AddRange(coffeeCategory, pastriesCategory);
   context.SaveChanges();

        data.CoffeeCategoryId = coffeeCategory.Id;
        data.PastriesCategoryId = pastriesCategory.Id;

        // Products
    var latte = new Product
 {
         CategoryId = coffeeCategory.Id,
    Name = "Latte",
            Description = "Espresso with steamed milk",
            IsAvailable = true
        };

   var croissant = new Product
        {
   CategoryId = pastriesCategory.Id,
Name = "Croissant",
        Description = "Buttery French pastry",
   IsAvailable = true
        };

        var unavailableProduct = new Product
        {
     CategoryId = coffeeCategory.Id,
    Name = "Seasonal Special",
            Description = "Currently unavailable",
            IsAvailable = false
        };

    context.Products.AddRange(latte, croissant, unavailableProduct);
        context.SaveChanges();

        data.LatteProductId = latte.Id;
        data.CroissantProductId = croissant.Id;
        data.UnavailableProductId = unavailableProduct.Id;

        // Variants
      var latteSmall = new ProductVariant
        {
            ProductId = latte.Id,
         Name = "Small",
   Price = 100.00m,
      IsDefault = true,
            IsAvailable = true
        };

        var latteLarge = new ProductVariant
        {
 ProductId = latte.Id,
            Name = "Large",
      Price = 150.00m,
            IsDefault = false,
    IsAvailable = true
        };

        var croissantDefault = new ProductVariant
        {
            ProductId = croissant.Id,
Name = "Default",
Price = 80.00m,
            IsDefault = true,
IsAvailable = true
        };

        var unavailableVariant = new ProductVariant
        {
            ProductId = unavailableProduct.Id,
  Name = "Regular",
         Price = 120.00m,
            IsDefault = true,
     IsAvailable = true
        };

      context.ProductVariants.AddRange(latteSmall, latteLarge, croissantDefault, unavailableVariant);
        context.SaveChanges();

        data.LatteSmallVariantId = latteSmall.Id;
 data.LatteLargeVariantId = latteLarge.Id;
        data.CroissantDefaultVariantId = croissantDefault.Id;
      data.UnavailableProductVariantId = unavailableVariant.Id;

 // Addons
   var extraShot = new Addon
        {
  Name = "Extra Shot",
  Price = 30.00m,
     Group = "Espresso",
            MaxPerItem = 3,
            IsAvailable = true
        };

        var oatMilk = new Addon
        {
    Name = "Oat Milk",
    Price = 20.00m,
      Group = "Milk",
            MaxPerItem = 1,
 IsAvailable = true
        };

        var unavailableAddon = new Addon
        {
 Name = "Seasonal Syrup",
 Price = 25.00m,
            Group = "Syrup",
       MaxPerItem = 2,
       IsAvailable = false
        };

        context.Addons.AddRange(extraShot, oatMilk, unavailableAddon);
     context.SaveChanges();

     data.ExtraShotAddonId = extraShot.Id;
        data.OatMilkAddonId = oatMilk.Id;
        data.UnavailableAddonId = unavailableAddon.Id;

        // ProductAddon mappings - Latte allows Extra Shot, Oat Milk, and Seasonal Syrup (unavailable)
 // Croissant allows NONE
      context.ProductAddons.AddRange(
            new ProductAddon { ProductId = latte.Id, AddonId = extraShot.Id },
       new ProductAddon { ProductId = latte.Id, AddonId = oatMilk.Id },
            new ProductAddon { ProductId = latte.Id, AddonId = unavailableAddon.Id }
        );
        context.SaveChanges();

        return data;
    }
}
