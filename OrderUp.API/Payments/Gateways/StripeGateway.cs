using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.API.Payments.Abstractions;
using Stripe;
using Stripe.Checkout;

namespace OrderUp.API.Payments.Gateways;

/// <summary>
/// Stripe payment gateway implementation.
/// </summary>
public class StripeGateway : IPaymentGateway
{
    public string Name => "Stripe";

    private readonly StripeOptions _stripeOptions;
    private readonly PaymentsOptions _paymentsOptions;
    private readonly DataContext _dbContext;
    private readonly ILogger<StripeGateway> _logger;

    public StripeGateway(
        IOptions<StripeOptions> stripeOptions,
        IOptions<PaymentsOptions> paymentsOptions,
        DataContext dbContext,
        ILogger<StripeGateway> logger)
    {
        _stripeOptions = stripeOptions.Value;
        _paymentsOptions = paymentsOptions.Value;
        _dbContext = dbContext;
        _logger = logger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _stripeOptions.SecretKey;
    }

    public async Task<(string CheckoutUrl, string SessionId)> CreateCheckoutAsync(
        Order order,
        CancellationToken ct = default)
    {
        var lineItems = new List<SessionLineItemOptions>();

        foreach (var item in order.Items)
        {
            // Base item price
            var unitAmount = (long)(item.BaseUnitPriceSnapshot * 100); // Stripe uses cents

            // Add addon prices to unit amount
            foreach (var addon in item.Addons)
            {
                unitAmount += (long)(addon.UnitPriceSnapshot * addon.Quantity * 100);
            }

            // Build description with addons
            var description = item.VariantNameSnapshot;
            if (item.Addons.Any())
            {
                var addonNames = item.Addons.Select(a =>
                    a.Quantity > 1 ? $"{a.Quantity}x {a.NameSnapshot}" : a.NameSnapshot);
                description += $" + {string.Join(", ", addonNames)}";
            }

            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = _paymentsOptions.Currency.ToLower(),
                    UnitAmount = unitAmount,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.ProductNameSnapshot,
                        Description = description
                    }
                },
                Quantity = item.Quantity
            });
        }

        var successUrl = $"{_paymentsOptions.PublicBaseUrl}/order/{order.Id}?payment=success";
        var cancelUrl = $"{_paymentsOptions.PublicBaseUrl}/order/{order.Id}?payment=cancelled";

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.ToString()
            },
            CustomerEmail = null, // Could add if we collect email
            PaymentMethodTypes = ["card"]
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Created Stripe checkout session {SessionId} for order {OrderId}",
            session.Id, 
            order.Id);

        return (session.Url!, session.Id);
    }

    public async Task HandleWebhookAsync(HttpRequest request, CancellationToken ct = default)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync(ct);
        var signature = request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            throw new InvalidOperationException("Missing Stripe-Signature header");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _stripeOptions.WebhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            throw new InvalidOperationException("Invalid webhook signature", ex);
        }

        _logger.LogInformation(
            "Received Stripe webhook event {EventType} with ID {EventId}",
            stripeEvent.Type, 
            stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                await HandleCheckoutSessionCompleted(stripeEvent, ct);
                break;

            case EventTypes.CheckoutSessionExpired:
                await HandleCheckoutSessionExpired(stripeEvent, ct);
                break;

            default:
                _logger.LogDebug("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            _logger.LogWarning("Could not deserialize checkout session from webhook");
            return;
        }

        if (!session.Metadata.TryGetValue("order_id", out var orderIdStr) ||
            !int.TryParse(orderIdStr, out var orderId))
        {
            _logger.LogWarning("Missing or invalid order_id in session metadata");
            return;
        }

        var order = await _dbContext.Orders.FindAsync([orderId], ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for completed session", orderId);
            return;
        }

        // Idempotency check
        if (order.PaymentStatus == Data.Entities.PaymentStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} already marked as paid, skipping", orderId);
            return;
        }

        order.PaymentStatus = Data.Entities.PaymentStatus.Paid;
        order.PaymentIntentId = session.PaymentIntentId;
        order.PaymentLastEventAtUtc = DateTime.UtcNow;

        // Auto-confirm order when paid
        if (order.Status == Data.Entities.OrderStatus.Pending)
        {
            order.Status = Data.Entities.OrderStatus.Confirmed;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderId} marked as Paid via Stripe session {SessionId}",
            orderId, 
            session.Id);
    }

    private async Task HandleCheckoutSessionExpired(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null) return;

        if (!session.Metadata.TryGetValue("order_id", out var orderIdStr) ||
            !int.TryParse(orderIdStr, out var orderId))
        {
            return;
        }

        var order = await _dbContext.Orders.FindAsync([orderId], ct);
        if (order is null) return;

        // Only update if still pending payment
        if (order.PaymentStatus == Data.Entities.PaymentStatus.Pending)
        {
            order.PaymentStatus = Data.Entities.PaymentStatus.Failed;
            order.PaymentLastEventAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Order {OrderId} payment session expired", 
                orderId);
        }
    }
}
