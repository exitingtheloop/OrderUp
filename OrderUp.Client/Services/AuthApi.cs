using System.Net.Http.Json;
using OrderUp.Shared.Contracts.Auth;

namespace OrderUp.Client.Services;

/// <summary>
/// Client for authentication API endpoints.
/// </summary>
public class AuthApi
{
    private readonly HttpClient _httpClient;

    public AuthApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Logs in with email and password.
    /// </summary>
    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var request = new LoginRequest(email, password, rememberMe);
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var status = await response.Content.ReadFromJsonAsync<AuthStatusResponse>();
                return new AuthResult(true, null, status);
            }

            // Try to read error message
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return new AuthResult(false, error?.Error ?? "Login failed.", null);
            }
            catch
            {
                return new AuthResult(false, "Login failed.", null);
            }
        }
        catch (Exception ex)
        {
            return new AuthResult(false, $"Network error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    public async Task<bool> LogoutAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/auth/logout", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current authentication status.
    /// </summary>
    public async Task<AuthStatusResponse?> GetStatusAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<AuthStatusResponse>("api/auth/status");
        }
        catch
        {
            return null;
        }
    }

    private record ErrorResponse(string? Error);
}

public record AuthResult(bool Success, string? ErrorMessage, AuthStatusResponse? Status);
