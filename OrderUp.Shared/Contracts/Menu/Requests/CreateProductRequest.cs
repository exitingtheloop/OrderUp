namespace OrderUp.Shared.Contracts.Menu.Requests;

public record CreateProductRequest(
    int CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsAvailable = true
);
