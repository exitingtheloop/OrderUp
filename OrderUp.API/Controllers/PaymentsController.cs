using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Payments;
using OrderUp.Shared.Contracts.Payments.Requests;
using OrderUp.Shared.Contracts.Payments.Responses;

namespace OrderUp.API.Controllers;

/// <summary>
/// Controller for payment operations.
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a checkout session for the specified order.
    /// Returns a URL to redirect the customer to the payment page.
    /// </summary>
    [HttpPost("checkout")]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        CreateCheckoutSessionRequest request,
        CancellationToken ct)
    {
        try
        {
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(request.OrderId, ct);
            return Ok(new CreateCheckoutSessionResponse(checkoutUrl));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create checkout session for order {OrderId}", request.OrderId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Webhook endpoint for payment provider callbacks.
    /// The provider name is used to route to the correct gateway.
    /// </summary>
    /// <remarks>
    /// Stripe sends webhooks to: POST /api/payments/webhook/stripe
    /// PayMongo sends webhooks to: POST /api/payments/webhook/paymongo
    /// </remarks>
    [HttpPost("webhook/{provider}")]
    public async Task<IActionResult> HandleWebhook(
        string provider,
        CancellationToken ct)
    {
        try
        {
            // Enable buffering so the body can be read multiple times if needed
            Request.EnableBuffering();

            await _paymentService.HandleWebhookAsync(provider, Request, ct);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Webhook handling failed for provider {Provider}", provider);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error handling webhook for provider {Provider}", provider);
            return StatusCode(500);
        }
    }
}
