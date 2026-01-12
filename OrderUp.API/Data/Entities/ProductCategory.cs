namespace OrderUp.API.Data.Entities;

public class ProductCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
