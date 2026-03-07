using Microsoft.JSInterop;
using System.Text.Json;

namespace OrderUp.Client.Services;

/// <summary>
/// Tracks recent orders created on this device via localStorage.
/// This allows guest users to find their orders without authentication.
/// </summary>
public class RecentOrdersService
{
    private readonly IJSRuntime _jsRuntime;
    private const string StorageKey = "orderup_recent_orders";
    private const int MaxRecentOrders = 10;

    public RecentOrdersService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Add an order ID to the recent orders list.
    /// Called after successful order creation.
    /// </summary>
    public async Task AddOrderAsync(int orderId)
    {
        var orders = await GetOrderIdsAsync();

        // Remove if already exists (will re-add at front)
        orders.Remove(orderId);

        // Add to front
        orders.Insert(0, orderId);

        // Keep only the most recent N orders
        if (orders.Count > MaxRecentOrders)
        {
            orders = orders.Take(MaxRecentOrders).ToList();
        }

        await SaveOrderIdsAsync(orders);
    }

    /// <summary>
    /// Get list of recent order IDs (most recent first).
    /// </summary>
    public async Task<List<int>> GetOrderIdsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrEmpty(json))
            {
                return new List<int>();
            }
            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }
        catch
        {
            return new List<int>();
        }
    }

    /// <summary>
    /// Remove an order ID from the list (e.g., if order not found/404).
    /// </summary>
    public async Task RemoveOrderAsync(int orderId)
    {
        var orders = await GetOrderIdsAsync();
        if (orders.Remove(orderId))
        {
            await SaveOrderIdsAsync(orders);
        }
    }

    /// <summary>
    /// Clear all recent orders.
    /// </summary>
    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private async Task SaveOrderIdsAsync(List<int> orderIds)
    {
        var json = JsonSerializer.Serialize(orderIds);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }
}
