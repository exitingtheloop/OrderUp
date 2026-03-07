namespace OrderUp.Shared.Contracts.Payments.Requests;

/// <summary>
/// Request to create a checkout session for an order.
/// </summary>
public record CreateCheckoutSessionRequest(int OrderId);
