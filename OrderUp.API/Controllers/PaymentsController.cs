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
    /// <param name="request">The checkout request containing the order ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Checkout URL to redirect customer to payment page</returns>
    /// <response code="200">Checkout session created successfully</response>
    /// <response code="400">Order not found, already paid, or has no items</response>
    /// <response code="409">Payment already initiated for this order (if strict mode enabled)</response>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CreateCheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession(
        CreateCheckoutSessionRequest request,
        CancellationToken ct)
    {
        try
        {
            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(request.OrderId, ct);
            return Ok(new CreateCheckoutSessionResponse(checkoutUrl));
        }
        catch (PaymentAlreadyInitiatedException ex)
        {
            _logger.LogInformation(ex, "Payment already initiated for order {OrderId}", request.OrderId);
            return Conflict(new { error = ex.Message });
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
    /// 
    /// Webhooks are unauthenticated by nature; security is via signature verification
    /// performed by each gateway implementation.
    /// 
    /// Returns 200 OK even if already processed (idempotency).
    /// </remarks>
    /// <param name="provider">The payment provider name (stripe or paymongo)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Webhook processed successfully (or already processed)</response>
    /// <response code="400">Invalid webhook signature or payload</response>
    [HttpPost("webhook/{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook(
        string provider,
        CancellationToken ct)
    {
        try
        {
            // Enable buffering so the body can be read multiple times if needed
            Request.EnableBuffering();

            await _paymentService.HandleWebhookAsync(provider, Request, ct);

            // Return 200 quickly even if already processed (idempotency)
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            // Invalid signature or unknown provider
            _logger.LogWarning(ex, "Webhook handling failed for provider {Provider}", provider);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log but don't expose internal errors
            _logger.LogError(ex, "Unexpected error handling webhook for provider {Provider}", provider);

            // Still return 200 to prevent retries for unrecoverable errors
            // Stripe/PayMongo will retry on 5xx, which we don't want for bugs
            return Ok();
        }
    }
}
