namespace OrderUp.API.Payments.Abstractions;

/// <summary>
/// Resolves the active payment gateway based on configuration.
/// </summary>
public interface IPaymentGatewayResolver
{
    /// <summary>
    /// Gets the currently configured payment gateway.
    /// </summary>
    /// <returns>The active payment gateway</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching gateway is found</exception>
    IPaymentGateway GetActiveGateway();

    /// <summary>
    /// Gets a payment gateway by name.
    /// </summary>
    /// <param name="name">Gateway name (e.g., "Stripe" or "PayMongo")</param>
    /// <returns>The gateway, or null if not found</returns>
    IPaymentGateway? GetGateway(string name);
}
