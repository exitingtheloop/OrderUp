namespace OrderUp.Shared.Contracts.Auth;

public record LoginRequest(
    string Email,
    string Password,
    bool RememberMe = false
);
