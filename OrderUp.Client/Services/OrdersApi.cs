using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;

namespace OrderUp.Client.Services;

/// <summary>
/// Client for order-related API calls.
/// </summary>
public class OrdersApi
{
    private readonly ApiClient _apiClient;

    public OrdersApi(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<CreateOrderResponse?> CreateOrderAsync(CreateOrderRequest request)
    {
        return await _apiClient.PostAsync<CreateOrderRequest, CreateOrderResponse>("api/orders", request);
    }

    public async Task<OrderDto?> GetOrderAsync(int orderId)
    {
        return await _apiClient.GetAsync<OrderDto>($"api/orders/{orderId}");
    }
}
