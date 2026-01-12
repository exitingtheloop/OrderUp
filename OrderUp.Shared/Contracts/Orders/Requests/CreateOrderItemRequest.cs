namespace OrderUp.Shared.Contracts.Orders.Requests;

public record CreateOrderItemRequest(
    int ProductId,
    int VariantId,
    int Quantity,
    string? Notes,
    List<CreateOrderItemAddonRequest> Addons
);
