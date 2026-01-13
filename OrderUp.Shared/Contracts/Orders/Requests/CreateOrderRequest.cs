namespace OrderUp.Shared.Contracts.Orders.Requests;

public record CreateOrderRequest(
    string CustomerName,
    string CustomerPhone,
    DateTime? PickupTimeUtc,
    List<CreateOrderItemRequest> Items
);
