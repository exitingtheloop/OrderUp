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
            // Main product line item
            // Name = ProductNameSnapshot + VariantNameSnapshot
            // UnitAmount = BaseUnitPriceSnapshot * 100 (Stripe uses smallest currency unit)
            var productName = $"{item.ProductNameSnapshot} ({item.VariantNameSnapshot})";
            var unitAmountCents = (long)(item.BaseUnitPriceSnapshot * 100);

            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = _paymentsOptions.Currency.ToLower(),
                    UnitAmount = unitAmountCents,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = productName
                    }
                },
                Quantity = item.Quantity
            });

            // Each addon becomes its own line item
            foreach (var addon in item.Addons)
            {
                var addonUnitAmountCents = (long)(addon.UnitPriceSnapshot * 100);

                // For addons, we need to account for:
                // - addon.Quantity = how many of this addon per item
                // - item.Quantity = how many items ordered
                // Total addon quantity = addon.Quantity * item.Quantity
                var totalAddonQuantity = addon.Quantity * item.Quantity;

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _paymentsOptions.Currency.ToLower(),
                        UnitAmount = addonUnitAmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = addon.NameSnapshot,
                            Description = $"Add-on for {item.ProductNameSnapshot}"
                        }
                    },
                    Quantity = totalAddonQuantity
                });
            }
        }

        var successUrl = $"{_paymentsOptions.PublicBaseUrl}/order-success/{order.Id}?paid=1";
        var cancelUrl = $"{_paymentsOptions.PublicBaseUrl}/order-success/{order.Id}?cancelled=1";

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            LineItems = lineItems,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = order.Id.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString()
            },
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
        // Read raw request body as string (required for signature verification)
        var json = await new StreamReader(request.Body).ReadToEndAsync(ct);

        // Get Stripe-Signature header
        var signature = request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Missing Stripe-Signature header in webhook request");
            throw new InvalidOperationException("Missing Stripe-Signature header");
        }

        // Verify with EventUtility.ConstructEvent(body, signature, WebhookSecret)
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

        // Handle event types
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

    /// <summary>
    /// Finds an order from a Stripe checkout session using a hybrid approach:
    /// 1. Fast path: Parse orderId from metadata, verify PaymentSessionId matches
    /// 2. Fallback: Query by PaymentSessionId directly
    /// </summary>
    private async Task<Order?> FindOrderFromSessionAsync(Session session, CancellationToken ct)
    {
        // Try 1: Fast path - parse orderId from metadata
        if (session.Metadata.TryGetValue("orderId", out var orderIdStr) &&
            int.TryParse(orderIdStr, out var orderId))
        {
            var order = await _dbContext.Orders.FindAsync([orderId], ct);

            if (order is not null)
            {
                // Security check: verify PaymentSessionId matches
                if (order.PaymentSessionId == session.Id)
                {
                    return order;
                }

                _logger.LogWarning(
                    "Order {OrderId} found but PaymentSessionId mismatch. Expected {Expected}, got {Actual}",
                    orderId,
                    session.Id,
                    order.PaymentSessionId);
            }
            else
            {
                _logger.LogWarning("Order {OrderId} from metadata not found in database", orderId);
            }
        }

        // Try 2: Fallback to ClientReferenceId
        if (!string.IsNullOrEmpty(session.ClientReferenceId) &&
            int.TryParse(session.ClientReferenceId, out var clientRefId))
        {
            var order = await _dbContext.Orders.FindAsync([clientRefId], ct);

            if (order is not null && order.PaymentSessionId == session.Id)
            {
                return order;
            }
        }

        // Try 3: Final fallback - query by PaymentSessionId directly
        // This handles edge cases where metadata might be corrupted
        var orderBySessionId = await _dbContext.Orders
            .FirstOrDefaultAsync(o => o.PaymentSessionId == session.Id, ct);

        if (orderBySessionId is not null)
        {
            _logger.LogInformation(
                "Found order {OrderId} via PaymentSessionId fallback query",
                orderBySessionId.Id);
            return orderBySessionId;
        }

        _logger.LogWarning(
            "Could not find order for session {SessionId}. Metadata orderId: {MetadataOrderId}, ClientReferenceId: {ClientReferenceId}",
            session.Id,
            orderIdStr ?? "null",
            session.ClientReferenceId ?? "null");

        return null;
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null)
        {
            _logger.LogWarning("Could not deserialize checkout session from webhook");
            return;
        }

        // Find order using hybrid approach
        var order = await FindOrderFromSessionAsync(session, ct);
        if (order is null)
        {
            return;
        }

        // Idempotency check: if already Paid, ignore
        if (order.PaymentStatus == PaymentStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} already marked as paid, skipping", order.Id);
            return;
        }

        // Check if payment is complete
        if (session.PaymentStatus == "paid")
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaymentIntentId = session.PaymentIntentId;
            order.PaymentLastEventAtUtc = DateTime.UtcNow;

            // Auto-confirm order when paid
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Confirmed;
            }

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Order {OrderId} marked as Paid via Stripe session {SessionId}",
                order.Id,
                session.Id);
        }
        else
        {
            _logger.LogInformation(
                "Checkout session {SessionId} completed but payment status is {PaymentStatus}",
                session.Id,
                session.PaymentStatus);
        }
    }

    private async Task HandleCheckoutSessionExpired(Event stripeEvent, CancellationToken ct)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session is null) return;

        // Find order using hybrid approach
        var order = await FindOrderFromSessionAsync(session, ct);
        if (order is null) return;

        // Only update if still pending payment (idempotency)
        if (order.PaymentStatus == PaymentStatus.Pending)
        {
            order.PaymentStatus = PaymentStatus.Failed;
            order.PaymentLastEventAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Order {OrderId} payment session expired", order.Id);
        }
    }
}
