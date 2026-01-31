namespace OrderUp.Shared.Contracts.Auth;

public record AuthStatusResponse(
    bool IsAuthenticated,
    string? Email,
    List<string> Roles
);
