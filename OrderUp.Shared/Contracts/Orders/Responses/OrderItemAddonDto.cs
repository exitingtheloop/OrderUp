namespace OrderUp.Shared.Contracts.Orders.Responses;

public record OrderItemAddonDto(
    int AddonId,
    string Name,
    string Group,
    int Quantity,
    decimal UnitPrice
);
