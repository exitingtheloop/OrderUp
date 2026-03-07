namespace OrderUp.Shared.Contracts;

/// <summary>
/// Application settings shared with the client.
/// </summary>
public record AppSettingsResponse(
    string CurrencySymbol,
    string CurrencyCode,
    string CafeName,
    string ReceiptFooter
);
