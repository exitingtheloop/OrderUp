namespace OrderUp.Shared.Contracts.Menu.Requests;

public record UpdateProductRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsAvailable,
    int CategoryId
);
