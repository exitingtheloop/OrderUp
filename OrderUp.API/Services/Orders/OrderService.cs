using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.API.Mapping;
using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;

namespace OrderUp.API.Services.Orders;

public class OrderService : IOrderService
{
    private readonly DataContext _context;

    public OrderService(DataContext context)
    {
        _context = context;
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        // Validate request has items
        if (request.Items.Count == 0)
            throw new InvalidOperationException("Order must contain at least one item.");

        // Load all required products, variants, and addons for validation
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var variantIds = request.Items.Select(i => i.VariantId).Distinct().ToList();
        var addonIds = request.Items
            .SelectMany(i => i.Addons)
            .Select(a => a.AddonId)
            .Distinct()
            .ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id);

        var addons = await _context.Addons
            .Where(a => addonIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id);

        // Load product-addon eligibility mappings for all requested products
        var productAddonMappings = await _context.ProductAddons
            .Where(pa => productIds.Contains(pa.ProductId))
            .ToListAsync();

        // Build a lookup: ProductId -> Set of allowed AddonIds
        var allowedAddonsByProduct = productAddonMappings
            .GroupBy(pa => pa.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(pa => pa.AddonId).ToHashSet()
            );

        // Validate and build order
        var order = new Order
        {
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            PickupTimeUtc = request.PickupTimeUtc,
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.Pending
        };

        foreach (var itemRequest in request.Items)
        {
            // Validate quantity
            if (itemRequest.Quantity <= 0) throw new InvalidOperationException($"Item quantity must be greater than 0.");

            // Validate product
            if (!products.TryGetValue(itemRequest.ProductId, out var product))
                throw new InvalidOperationException($"Product {itemRequest.ProductId} not found.");

            if (!product.IsAvailable)
                throw new InvalidOperationException($"Product '{product.Name}' is not available.");

            // Validate variant
            if (!variants.TryGetValue(itemRequest.VariantId, out var variant))
                throw new InvalidOperationException($"Variant {itemRequest.VariantId} not found.");

            if (variant.ProductId != product.Id)
                throw new InvalidOperationException($"Variant '{variant.Name}' does not belong to product '{product.Name}'.");

            if (!variant.IsAvailable)
                throw new InvalidOperationException($"Variant '{variant.Name}' is not available.");

            // Get allowed addons for this product (empty set if none configured)
            var allowedAddonIds = allowedAddonsByProduct.GetValueOrDefault(product.Id, []);

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                VariantId = variant.Id,
                Quantity = itemRequest.Quantity,
                Notes = itemRequest.Notes,
                BaseUnitPriceSnapshot = variant.Price,
                ProductNameSnapshot = product.Name,
                VariantNameSnapshot = variant.Name
            };

            // Validate and add addons
            foreach (var addonRequest in itemRequest.Addons)
            {
                if (addonRequest.Quantity <= 0)
                    throw new InvalidOperationException($"Addon quantity must be greater than 0.");

                if (!addons.TryGetValue(addonRequest.AddonId, out var addon))
                    throw new InvalidOperationException($"Addon {addonRequest.AddonId} not found.");

                // Validate addon eligibility for this product
                if (!allowedAddonIds.Contains(addon.Id))
                    throw new InvalidOperationException($"Addon '{addon.Name}' is not allowed for product '{product.Name}'.");

                if (!addon.IsAvailable)
                    throw new InvalidOperationException($"Addon '{addon.Name}' is not available.");

                orderItem.Addons.Add(new OrderItemAddon
                {
                    AddonId = addon.Id,
                    Quantity = addonRequest.Quantity,
                    UnitPriceSnapshot = addon.Price,
                    NameSnapshot = addon.Name,
                    GroupSnapshot = addon.Group
                });
            }

            order.Items.Add(orderItem);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return new CreateOrderResponse(order.Id);
    }

    public async Task<OrderDto?> GetOrderAsync(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Addons)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order?.ToDto();
    }

    public async Task<List<OrderDto>> GetTodaysOrdersAsync()
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        var orders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Addons)
            .Where(o => o.CreatedAtUtc >= todayUtc && o.CreatedAtUtc < tomorrowUtc)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

        return orders.Select(o => o.ToDto()).ToList();
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Addons)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
            return null;

        order.Status = (OrderStatus)request.Status;
        await _context.SaveChangesAsync();

        return order.ToDto();
    }
}
