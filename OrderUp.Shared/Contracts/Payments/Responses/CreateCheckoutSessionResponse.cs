namespace OrderUp.Shared.Contracts.Payments.Responses;

/// <summary>
/// Response containing the checkout URL for payment.
/// </summary>
public record CreateCheckoutSessionResponse(string CheckoutUrl);
