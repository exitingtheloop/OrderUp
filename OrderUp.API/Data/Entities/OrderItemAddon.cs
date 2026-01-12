namespace OrderUp.API.Data.Entities;

public class OrderItemAddon
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public int AddonId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string GroupSnapshot { get; set; } = string.Empty;

    public OrderItem OrderItem { get; set; } = null!;
    public Addon Addon { get; set; } = null!;
}
