using OrderUp.API.Data.Entities;

namespace OrderUp.API.Payments.Abstractions;

/// <summary>
/// Abstraction for payment gateway implementations.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Unique name of this gateway (e.g., "Stripe" or "PayMongo").
    /// Used for matching against configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Creates a checkout session and returns the URL to redirect the customer.
    /// </summary>
    /// <param name="order">The order to create checkout for (must include Items with snapshots)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (CheckoutUrl, SessionId)</returns>
    Task<(string CheckoutUrl, string SessionId)> CreateCheckoutAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// Handles incoming webhook from the payment provider.
    /// Verifies signature and updates order status accordingly.
    /// </summary>
    /// <param name="request">The incoming HTTP request with webhook payload</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleWebhookAsync(HttpRequest request, CancellationToken ct = default);
}
