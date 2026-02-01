using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.API.Payments.Abstractions;

namespace OrderUp.API.Payments;

/// <summary>
/// Façade service for payment operations.
/// Uses the configured gateway via PaymentGatewayResolver.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a checkout session for the specified order.
    /// </summary>
    /// <param name="orderId">The order ID to create checkout for</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The checkout URL to redirect the customer</returns>
    Task<string> CreateCheckoutSessionAsync(int orderId, CancellationToken ct = default);

    /// <summary>
    /// Handles a webhook from the specified payment provider.
    /// </summary>
    /// <param name="providerName">The provider name (from URL route)</param>
    /// <param name="request">The incoming HTTP request</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleWebhookAsync(string providerName, HttpRequest request, CancellationToken ct = default);
}

/// <summary>
/// Implementation of payment service façade.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentGatewayResolver _resolver;
    private readonly DataContext _dbContext;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentGatewayResolver resolver,
        DataContext dbContext,
        ILogger<PaymentService> logger)
    {
        _resolver = resolver;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<string> CreateCheckoutSessionAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Addons)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            throw new InvalidOperationException($"Order {orderId} not found");
        }

        // Validate order is not already paid
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            throw new InvalidOperationException("Order is already paid");
        }

        if (!order.Items.Any())
        {
            throw new InvalidOperationException("Order has no items");
        }

        var gateway = _resolver.GetActiveGateway();

        // Check if payment session already exists for this order with the same provider
        // This prevents creating duplicate checkout sessions
        if (!string.IsNullOrEmpty(order.PaymentSessionId) &&
            order.PaymentProvider == gateway.Name)
        {
            _logger.LogInformation(
                "Order {OrderId} already has a payment session {SessionId} with {Provider}. Recreating.",
                orderId, order.PaymentSessionId, gateway.Name);

            // Option 1: Return error (uncomment if you want stricter behavior)
            // throw new PaymentAlreadyInitiatedException($"Payment already initiated for order {orderId}");

            // Option 2: Recreate session (current behavior - more user-friendly)
            // User may have abandoned checkout and wants to try again
        }

        var (checkoutUrl, sessionId) = await gateway.CreateCheckoutAsync(order, ct);

        // Update order with payment session info
        order.PaymentProvider = gateway.Name;
        order.PaymentSessionId = sessionId;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created checkout session {SessionId} for order {OrderId} using {Provider}",
            sessionId, orderId, gateway.Name);

        return checkoutUrl;
    }

    public async Task HandleWebhookAsync(string providerName, HttpRequest request, CancellationToken ct = default)
    {
        var gateway = _resolver.GetGateway(providerName);

        if (gateway is null)
        {
            throw new InvalidOperationException($"Unknown payment provider: {providerName}");
        }

        await gateway.HandleWebhookAsync(request, ct);
    }
}

/// <summary>
/// Exception thrown when a payment has already been initiated for an order.
/// </summary>
public class PaymentAlreadyInitiatedException : InvalidOperationException
{
    public PaymentAlreadyInitiatedException(string message) : base(message) { }
}
