using OrderUp.Shared.Enums;

namespace OrderUp.Shared.Contracts.Orders.Responses;

/// <summary>
/// Lightweight order summary for listing orders.
/// </summary>
public record OrderSummaryDto(
    int Id,
    string CustomerName,
    DateTime CreatedAtUtc,
    DateTime? PickupTimeUtc,
    OrderStatus Status,
    PaymentStatus PaymentStatus,
    decimal Total,
    int ItemCount
);
