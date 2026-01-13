namespace OrderUp.Shared.Contracts.Menu.Responses;

public record AddonDto(
    int Id,
    string Name,
    decimal Price,
    string Group,
    int? MaxPerItem,
    bool IsAvailable
);
