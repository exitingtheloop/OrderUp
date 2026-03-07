using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Services.Orders;
using OrderUp.Shared.Contracts.Orders.Requests;
using OrderUp.Shared.Contracts.Orders.Responses;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(CreateOrderRequest request)
    {
        try
        {
            var response = await _orderService.CreateOrderAsync(request);
            return CreatedAtAction(nameof(GetOrder), new { id = response.OrderId }, response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);

        if (order is null) return NotFound();

        return Ok(order);
    }
}
