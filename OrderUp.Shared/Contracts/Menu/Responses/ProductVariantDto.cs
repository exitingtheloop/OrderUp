namespace OrderUp.Shared.Contracts.Menu.Responses;

public record ProductVariantDto(
    int Id,
    string Name,
    decimal Price,
    bool IsDefault,
    bool IsAvailable
);
