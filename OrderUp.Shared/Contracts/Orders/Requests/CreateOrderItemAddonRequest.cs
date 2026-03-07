namespace OrderUp.Shared.Contracts.Orders.Requests;

public record CreateOrderItemAddonRequest(
    int AddonId,
    int Quantity
);
