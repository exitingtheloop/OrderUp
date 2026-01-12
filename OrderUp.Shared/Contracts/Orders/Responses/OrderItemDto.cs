namespace OrderUp.Shared.Contracts.Orders.Responses;

public record OrderItemDto(
    int ProductId,
    string ProductName,
    int VariantId,
    string VariantName,
    int Quantity,
    decimal BaseUnitPrice,
    string? Notes,
    List<OrderItemAddonDto> Addons
);
