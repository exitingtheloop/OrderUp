using System.Net.Http.Json;
using OrderUp.Shared.Contracts.Payments.Requests;
using OrderUp.Shared.Contracts.Payments.Responses;

namespace OrderUp.Client.Services;

/// <summary>
/// Client for payment API endpoints.
/// </summary>
public class PaymentsApi
{
    private readonly HttpClient _httpClient;

    public PaymentsApi(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Creates a checkout session for the specified order.
    /// Returns the URL to redirect the customer to the payment page.
    /// </summary>
    public async Task<string?> CreateCheckoutSessionAsync(int orderId)
    {
        var request = new CreateCheckoutSessionRequest(orderId);
        var response = await _httpClient.PostAsJsonAsync("api/payments/checkout", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<CreateCheckoutSessionResponse>();
        return result?.CheckoutUrl;
    }
}
