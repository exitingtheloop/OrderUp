namespace OrderUp.Shared.Contracts.Menu.Requests;

public record CreateAddonRequest(
    string Name,
    decimal Price,
    string Group,
    int? MaxPerItem = null,
    bool IsAvailable = true
);
