namespace OrderUp.API.Payments;

/// <summary>
/// PayMongo-specific configuration options.
/// </summary>
public class PayMongoOptions
{
    public const string SectionName = "PayMongo";

    /// <summary>
    /// PayMongo secret API key (sk_test_... or sk_live_...).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// PayMongo webhook signing secret.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

  /// <summary>
    /// Allowed payment method types for checkout.
    /// Examples: "card", "gcash", "grab_pay", "paymaya"
    /// </summary>
    public string[] PaymentMethodTypes { get; set; } = ["card", "gcash"];
}
