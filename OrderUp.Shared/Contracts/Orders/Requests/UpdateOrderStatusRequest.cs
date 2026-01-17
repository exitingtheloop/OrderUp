using OrderUp.Shared.Enums;

namespace OrderUp.Shared.Contracts.Orders.Requests;

public record UpdateOrderStatusRequest(OrderStatus Status);
