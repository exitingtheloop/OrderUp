using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Data.Seed;
using OrderUp.API.Services.Orders;
using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = IdentitySeeder.AdminRole)]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Gets all orders created today (UTC), including items and addons.
    /// </summary>
    [HttpGet("today")]
    public async Task<ActionResult<List<OrderDto>>> GetTodaysOrders()
    {
        var orders = await _orderService.GetTodaysOrdersAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Updates the status of an order.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, UpdateOrderStatusRequest request)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, request);

            if (order is null)
                return NotFound(new { error = $"Order {id} not found." });

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
