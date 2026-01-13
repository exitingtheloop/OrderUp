namespace OrderUp.Shared.Contracts.Menu.Responses;

public record CategoryDto(
    int Id,
    string Name,
    int DisplayOrder
);
