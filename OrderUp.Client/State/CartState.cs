namespace OrderUp.Client.State;

/// <summary>
/// Manages the shopping cart state.
/// </summary>
public class CartState
{
    private readonly List<CartItem> _items = [];

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public event Action? OnChange;

    public void AddItem(CartItem item)
    {
        _items.Add(item);
        NotifyStateChanged();
    }

    public void RemoveItem(CartItem item)
    {
        _items.Remove(item);
        NotifyStateChanged();
    }

    public void Clear()
    {
        _items.Clear();
        NotifyStateChanged();
    }

    public int ItemCount => _items.Sum(i => i.Quantity);

    public decimal Total => _items.Sum(i => i.LineTotal);

    private void NotifyStateChanged() => OnChange?.Invoke();
}

/// <summary>
/// Represents an item in the cart with selected variant and addons.
/// </summary>
public class CartItem
{
    public required int ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int VariantId { get; init; }
    public required string VariantName { get; init; }
    public required decimal VariantPrice { get; init; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public List<CartItemAddon> Addons { get; init; } = [];

    public decimal LineTotal
    {
        get
        {
            var addonTotal = Addons.Sum(a => a.Price * a.Quantity);
            return (VariantPrice + addonTotal) * Quantity;
        }
    }
}

/// <summary>
/// Represents a selected addon for a cart item.
/// </summary>
public class CartItemAddon
{
    public required int AddonId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public int Quantity { get; set; } = 1;
}
