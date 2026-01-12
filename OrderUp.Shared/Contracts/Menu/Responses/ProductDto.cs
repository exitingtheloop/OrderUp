namespace OrderUp.Shared.Contracts.Menu.Responses;

public record ProductDto(
    int Id,
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsAvailable,
    int CategoryId,
    string CategoryName,
    List<ProductVariantDto> Variants
);
