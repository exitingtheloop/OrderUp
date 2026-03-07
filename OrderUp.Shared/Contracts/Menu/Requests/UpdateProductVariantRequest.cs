namespace OrderUp.Shared.Contracts.Menu.Requests;

public record UpdateProductVariantRequest(
    string Name,
    decimal Price,
    bool IsDefault,
    bool IsAvailable
);
