using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;

namespace OrderUp.API.Services.Orders;

public interface IOrderService
{
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderDto?> GetOrderAsync(int id);
    Task<List<OrderDto>> GetTodaysOrdersAsync();
    Task<OrderDto?> UpdateOrderStatusAsync(int id, UpdateOrderStatusRequest request);
}
