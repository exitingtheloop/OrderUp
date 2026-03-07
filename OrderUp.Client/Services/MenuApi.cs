using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.Client.Services;

/// <summary>
/// Client for menu-related API calls.
/// </summary>
public class MenuApi
{
    private readonly ApiClient _apiClient;

    public MenuApi(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<MenuDto?> GetMenuAsync()
    {
        return await _apiClient.GetAsync<MenuDto>("api/menu");
    }
}
