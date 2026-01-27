namespace OrderUp.Shared.Contracts.Menu.Requests;

public record CreateProductVariantRequest(
    string Name,
    decimal Price,
    bool IsDefault = false,
    bool IsAvailable = true
);
