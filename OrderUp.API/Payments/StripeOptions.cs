namespace OrderUp.API.Payments;

/// <summary>
/// Stripe-specific configuration options.
/// </summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Stripe secret API key (sk_test_... or sk_live_...).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe webhook signing secret (whsec_...).
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
