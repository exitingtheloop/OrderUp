namespace OrderUp.Shared.Contracts.Menu.Responses;

public record MenuDto(
    List<CategoryDto> Categories,
    List<ProductDto> Products
);
