namespace OrderUp.API.Data.Entities;

public class Addon
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Group { get; set; } = string.Empty;
    public int? MaxPerItem { get; set; }
    public bool IsAvailable { get; set; } = true;

    public ICollection<ProductAddon> ProductAddons { get; set; } = new List<ProductAddon>();
}
