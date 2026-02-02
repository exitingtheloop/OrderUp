using Microsoft.JSInterop;

namespace OrderUp.Client.Services;

/// <summary>
/// Service to track pending orders in browser localStorage.
/// Helps users find their orders if they navigate away during payment.
/// </summary>
public class PendingOrderService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "orderup_pending_order";
    private const string PhoneKey = "orderup_customer_phone";

    public PendingOrderService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Store a pending order ID before redirecting to payment.
    /// </summary>
    public async Task SetPendingOrderAsync(int orderId, string customerPhone)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, orderId.ToString());
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", PhoneKey, customerPhone);
    }

    /// <summary>
    /// Get the pending order ID, if any.
    /// </summary>
    public async Task<int?> GetPendingOrderIdAsync()
    {
        var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (int.TryParse(value, out var orderId))
        {
            return orderId;
        }
        return null;
    }

    /// <summary>
    /// Get the stored customer phone number.
    /// </summary>
    public async Task<string?> GetStoredPhoneAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", PhoneKey);
    }

    /// <summary>
    /// Clear the pending order (call after successful payment or cancellation).
    /// </summary>
    public async Task ClearPendingOrderAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    /// <summary>
    /// Clear all stored data.
    /// </summary>
    public async Task ClearAllAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", PhoneKey);
    }
}
