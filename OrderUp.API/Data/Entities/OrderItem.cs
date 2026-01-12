namespace OrderUp.API.Data.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public decimal BaseUnitPriceSnapshot { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string VariantNameSnapshot { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
    public ICollection<OrderItemAddon> Addons { get; set; } = new List<OrderItemAddon>();
}
