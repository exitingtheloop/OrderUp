namespace OrderUp.API.Data.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty; // Small, Medium, Large
    public decimal Price { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;

    public Product Product { get; set; } = null!;
}
