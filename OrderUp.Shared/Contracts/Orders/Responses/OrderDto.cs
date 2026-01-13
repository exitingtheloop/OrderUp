using OrderUp.Shared.Enums;

namespace OrderUp.Shared.Contracts.Orders.Responses;

public record OrderDto(
    int Id,
    string CustomerName,
    string CustomerPhone,
    DateTime CreatedAtUtc,
    DateTime? PickupTimeUtc,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal Total,
    List<OrderItemDto> Items
);
