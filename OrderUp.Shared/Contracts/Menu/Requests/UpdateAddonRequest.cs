namespace OrderUp.Shared.Contracts.Menu.Requests;

public record UpdateAddonRequest(
    string Name,
    decimal Price,
    string Group,
    int? MaxPerItem,
    bool IsAvailable
);
