namespace OrderUp.API.Payments;

/// <summary>
/// Global payment configuration options.
/// </summary>
public class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// The active payment provider name ("Stripe" or "PayMongo").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Public base URL of the application (used for success/cancel URLs).
    /// Example: "https://orderup.example.com"
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Default currency code (ISO 4217). Defaults to "PHP".
    /// </summary>
    public string Currency { get; set; } = "PHP";
}
