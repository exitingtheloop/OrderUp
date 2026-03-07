namespace OrderUp.API.Data.Entities;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PickupTimeUtc { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    // Payment provider fields
    /// <summary>
    /// The payment provider used (Stripe, PayMongo, etc.)
    /// </summary>
    public string? PaymentProvider { get; set; }

    /// <summary>
    /// Checkout session ID from the payment provider (Stripe Session ID or PayMongo Checkout Session ID)
    /// </summary>
    public string? PaymentSessionId { get; set; }

    /// <summary>
    /// Payment intent/transaction ID from the provider (Stripe payment_intent, PayMongo payment ID)
    /// </summary>
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Timestamp of the last webhook event processed for this order (for audit)
    /// </summary>
    public DateTime? PaymentLastEventAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
