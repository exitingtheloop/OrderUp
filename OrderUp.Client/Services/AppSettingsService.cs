using OrderUp.Shared.Contracts;
using System.Net.Http.Json;

namespace OrderUp.Client.Services;

/// <summary>
/// Service to fetch and cache application settings from the API.
/// Uses static cache so settings persist across scoped service instances.
/// </summary>
public class AppSettingsService
{
    private readonly HttpClient _httpClient;

    // Static cache - shared across all instances
    private static AppSettingsResponse? _cachedSettings;
    private static bool _isInitialized;
    private static readonly object _lock = new();

    // Default fallback values
    private const string DefaultCurrencySymbol = "?";
    private const string DefaultCurrencyCode = "PHP";

    public AppSettingsService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string CurrencySymbol => _cachedSettings?.CurrencySymbol ?? DefaultCurrencySymbol;
    public string CurrencyCode => _cachedSettings?.CurrencyCode ?? DefaultCurrencyCode;

    public async Task InitializeAsync()
    {
        // Double-check locking for thread safety
        if (_isInitialized) return;

        lock (_lock)
        {
            if (_isInitialized) return;
        }

        try
        {
            var settings = await _httpClient.GetFromJsonAsync<AppSettingsResponse>("api/settings");

            lock (_lock)
            {
                _cachedSettings = settings;
                _isInitialized = true;
            }
        }
        catch
        {
            // Use defaults if API call fails
            lock (_lock)
            {
                _cachedSettings = new AppSettingsResponse(DefaultCurrencySymbol, DefaultCurrencyCode);
                _isInitialized = true;
            }
        }
    }
}
