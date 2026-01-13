using OrderUp.Shared.Contracts.Orders.Responses;
using OrderUp.Shared.Enums;
using Order = OrderUp.API.Data.Entities.Order;
using OrderItem = OrderUp.API.Data.Entities.OrderItem;
using OrderItemAddon = OrderUp.API.Data.Entities.OrderItemAddon;

namespace OrderUp.API.Mapping;

public static class OrderMapping
{
    public static OrderItemAddonDto ToDto(this OrderItemAddon addon)
    {
        return new OrderItemAddonDto(
            addon.AddonId,
            addon.NameSnapshot,
            addon.GroupSnapshot,
            addon.Quantity,
            addon.UnitPriceSnapshot
        );
    }

    public static OrderItemDto ToDto(this OrderItem item)
    {
        return new OrderItemDto(
            item.ProductId,
            item.ProductNameSnapshot,
            item.VariantId,
            item.VariantNameSnapshot,
            item.Quantity,
            item.BaseUnitPriceSnapshot,
            item.Notes,
            item.Addons.Select(a => a.ToDto()).ToList()
        );
    }

    public static OrderDto ToDto(this Order order)
    {
        var items = order.Items.Select(i => i.ToDto()).ToList();
        var total = CalculateTotal(order);

        return new OrderDto(
            order.Id,
            order.CustomerName,
            order.CustomerPhone,
            order.CreatedAtUtc,
            order.PickupTimeUtc,
            (OrderStatus)order.Status,
            (PaymentStatus)order.PaymentStatus,
            total,
            items
        );
    }

    private static decimal CalculateTotal(Order order)
    {
        decimal total = 0;
        foreach (var item in order.Items)
        {
            var itemTotal = item.BaseUnitPriceSnapshot;
            foreach (var addon in item.Addons)
            {
                itemTotal += addon.UnitPriceSnapshot * addon.Quantity;
            }
            total += itemTotal * item.Quantity;
        }
        return total;
    }
}
