using OrderUp.Client.Services;
using OrderUp.Shared.Contracts.Auth;

namespace OrderUp.Client.State;

/// <summary>
/// Manages authentication state across the application.
/// </summary>
public class AuthState
{
    private readonly AuthApi _authApi;

    public bool IsAuthenticated { get; private set; }
    public string? Email { get; private set; }
    public List<string> Roles { get; private set; } = [];
    public bool IsAdmin => Roles.Contains("Admin");
    public bool IsInitialized { get; private set; }

    public event Action? OnChange;

    public AuthState(AuthApi authApi)
    {
        _authApi = authApi;
    }

    /// <summary>
    /// Initializes auth state by checking current status with the server.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        var status = await _authApi.GetStatusAsync();
        UpdateFromStatus(status);
        IsInitialized = true;
    }

    /// <summary>
    /// Attempts to log in with the provided credentials.
    /// </summary>
    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        var result = await _authApi.LoginAsync(email, password, rememberMe);

        if (result.Success && result.Status is not null)
        {
            UpdateFromStatus(result.Status);
        }

        return result;
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task LogoutAsync()
    {
        await _authApi.LogoutAsync();
        ClearState();
    }

    /// <summary>
    /// Refreshes the auth state from the server.
    /// </summary>
    public async Task RefreshAsync()
    {
        var status = await _authApi.GetStatusAsync();
        UpdateFromStatus(status);
    }

    private void UpdateFromStatus(AuthStatusResponse? status)
    {
        if (status is not null)
        {
            IsAuthenticated = status.IsAuthenticated;
            Email = status.Email;
            Roles = status.Roles;
        }
        else
        {
            ClearState();
        }

        NotifyStateChanged();
    }

    private void ClearState()
    {
        IsAuthenticated = false;
        Email = null;
        Roles = [];
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
