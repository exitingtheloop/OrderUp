using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.API.Payments.Abstractions;

namespace OrderUp.API.Payments.Gateways;

/// <summary>
/// PayMongo payment gateway implementation.
/// </summary>
public class PayMongoGateway : IPaymentGateway
{
    public string Name => "PayMongo";

    private readonly HttpClient _httpClient;
    private readonly PayMongoOptions _payMongoOptions;
    private readonly PaymentsOptions _paymentsOptions;
    private readonly DataContext _dbContext;
    private readonly ILogger<PayMongoGateway> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public PayMongoGateway(
        HttpClient httpClient,
        IOptions<PayMongoOptions> payMongoOptions,
        IOptions<PaymentsOptions> paymentsOptions,
        DataContext dbContext,
        ILogger<PayMongoGateway> logger)
    {
        _httpClient = httpClient;
        _payMongoOptions = payMongoOptions.Value;
        _paymentsOptions = paymentsOptions.Value;
        _dbContext = dbContext;
        _logger = logger;

        // Configure HttpClient for PayMongo API
        _httpClient.BaseAddress = new Uri("https://api.paymongo.com/v1/");
        var authBytes = Encoding.UTF8.GetBytes($"{_payMongoOptions.SecretKey}:");
        _httpClient.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
    }

    public async Task<(string CheckoutUrl, string SessionId)> CreateCheckoutAsync(
        Order order,
        CancellationToken ct = default)
    {
        var lineItems = new List<PayMongoLineItem>();

        foreach (var item in order.Items)
        {
            // Calculate total amount for item including addons (in centavos)
            var unitAmount = (int)(item.BaseUnitPriceSnapshot * 100);

            foreach (var addon in item.Addons)
            {
                unitAmount += (int)(addon.UnitPriceSnapshot * addon.Quantity * 100);
            }

            // Build description with addons
            var description = item.VariantNameSnapshot;
            if (item.Addons.Any())
            {
                var addonNames = item.Addons.Select(a =>
                       a.Quantity > 1 ? $"{a.Quantity}x {a.NameSnapshot}" : a.NameSnapshot);
                description += $" + {string.Join(", ", addonNames)}";
            }

            lineItems.Add(new PayMongoLineItem
            {
                Name = item.ProductNameSnapshot,
                Description = description,
                Amount = unitAmount * item.Quantity, // Total for this line
                Currency = _paymentsOptions.Currency.ToUpper(),
                Quantity = item.Quantity
            });
        }

        var successUrl = $"{_paymentsOptions.PublicBaseUrl}/order/{order.Id}?payment=success";
        var cancelUrl = $"{_paymentsOptions.PublicBaseUrl}/order/{order.Id}?payment=cancelled";

        var requestBody = new PayMongoCheckoutRequest
        {
            Data = new PayMongoCheckoutData
            {
                Attributes = new PayMongoCheckoutAttributes
                {
                    LineItems = lineItems,
                    PaymentMethodTypes = _payMongoOptions.PaymentMethodTypes,
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Description = $"Order #{order.Id}",
                    Metadata = new Dictionary<string, string>
                    {
                        ["order_id"] = order.Id.ToString()
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("checkout_sessions", content, ct);
        var responseJson = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                 "PayMongo checkout creation failed: {StatusCode} - {Response}",
                  response.StatusCode, responseJson);
            throw new InvalidOperationException($"PayMongo API error: {response.StatusCode}");
        }

        var result = JsonSerializer.Deserialize<PayMongoCheckoutResponse>(responseJson, JsonOptions);
        var checkoutUrl = result?.Data?.Attributes?.CheckoutUrl;
        var sessionId = result?.Data?.Id;

        if (string.IsNullOrEmpty(checkoutUrl) || string.IsNullOrEmpty(sessionId))
        {
            throw new InvalidOperationException("Invalid PayMongo response: missing checkout URL or session ID");
        }

        _logger.LogInformation(
            "Created PayMongo checkout session {SessionId} for order {OrderId}",
            sessionId, 
            order.Id);

        return (checkoutUrl, sessionId);
    }

    public async Task HandleWebhookAsync(HttpRequest request, CancellationToken ct = default)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync(ct);
        var signature = request.Headers["Paymongo-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            throw new InvalidOperationException("Missing Paymongo-Signature header");
        }

        // Verify webhook signature
        if (!VerifySignature(json, signature))
        {
            _logger.LogWarning("Invalid PayMongo webhook signature");
            throw new InvalidOperationException("Invalid webhook signature");
        }

        var webhookEvent = JsonSerializer.Deserialize<PayMongoWebhookEvent>(json, JsonOptions);
        if (webhookEvent is null)
        {
            throw new InvalidOperationException("Could not deserialize webhook payload");
        }

        var eventType = webhookEvent.Data?.Attributes?.Type;
        _logger.LogInformation(
            "Received PayMongo webhook event {EventType} with ID {EventId}",
            eventType, 
            webhookEvent.Data?.Id);

        switch (eventType)
        {
            case "checkout_session.payment.paid":
                await HandleCheckoutSessionPaid(webhookEvent, ct);
                break;

            case "checkout_session.payment.failed":
                await HandleCheckoutSessionFailed(webhookEvent, ct);
                break;

            default:
                _logger.LogDebug("Unhandled PayMongo event type: {EventType}", eventType);
                break;
        }
    }

    private bool VerifySignature(string payload, string signatureHeader)
    {
        // PayMongo signature format: t=<timestamp>,te=<test_signature>,li=<live_signature>
        var parts = signatureHeader.Split(',')
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0], p => p[1]);

        if (!parts.TryGetValue("t", out var timestamp))
        {
            return false;
        }

        // Get the appropriate signature (te for test, li for live)
        var expectedSignature = parts.GetValueOrDefault("te") ?? parts.GetValueOrDefault("li");
        if (string.IsNullOrEmpty(expectedSignature))
        {
            return false;
        }

        // Compute HMAC-SHA256
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_payMongoOptions.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return string.Equals(computedSignature, expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleCheckoutSessionPaid(PayMongoWebhookEvent webhookEvent, CancellationToken ct)
    {
        var checkoutData = webhookEvent.Data?.Attributes?.Data;
        var metadata = checkoutData?.Attributes?.Metadata;

        if (metadata is null || !metadata.TryGetValue("order_id", out var orderIdStr) ||
            !int.TryParse(orderIdStr, out var orderId))
        {
            _logger.LogWarning("Missing or invalid order_id in PayMongo webhook metadata");
            return;
        }

        var order = await _dbContext.Orders.FindAsync([orderId], ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for PayMongo webhook", orderId);
            return;
        }

        // Idempotency check
        if (order.PaymentStatus == Data.Entities.PaymentStatus.Paid)
        {
            _logger.LogInformation("Order {OrderId} already marked as paid, skipping", orderId);
            return;
        }

        order.PaymentStatus = Data.Entities.PaymentStatus.Paid;
        order.PaymentIntentId = checkoutData?.Attributes?.PaymentIntentId;
        order.PaymentLastEventAtUtc = DateTime.UtcNow;

        // Auto-confirm order when paid
        if (order.Status == Data.Entities.OrderStatus.Pending)
        {
            order.Status = Data.Entities.OrderStatus.Confirmed;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderId} marked as Paid via PayMongo", 
            orderId);
    }

    private async Task HandleCheckoutSessionFailed(PayMongoWebhookEvent webhookEvent, CancellationToken ct)
    {
        var checkoutData = webhookEvent.Data?.Attributes?.Data;
        var metadata = checkoutData?.Attributes?.Metadata;

        if (metadata is null || !metadata.TryGetValue("order_id", out var orderIdStr) ||
           !int.TryParse(orderIdStr, out var orderId))
        {
            return;
        }

        var order = await _dbContext.Orders.FindAsync([orderId], ct);
        if (order is null) return;

        // Only update if still pending payment
        if (order.PaymentStatus == Data.Entities.PaymentStatus.Pending)
        {
            order.PaymentStatus = Data.Entities.PaymentStatus.Failed;
            order.PaymentLastEventAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Order {OrderId} payment failed via PayMongo", orderId);
        }
    }

    #region PayMongo DTOs

    private class PayMongoCheckoutRequest
    {
        public PayMongoCheckoutData Data { get; set; } = new();
    }

    private class PayMongoCheckoutData
    {
        public PayMongoCheckoutAttributes Attributes { get; set; } = new();
    }

    private class PayMongoCheckoutAttributes
    {
        public List<PayMongoLineItem> LineItems { get; set; } = [];
        public string[] PaymentMethodTypes { get; set; } = [];
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? PaymentIntentId { get; set; }
    }

    private class PayMongoLineItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; } = "PHP";
        public int Quantity { get; set; }
    }

    private class PayMongoCheckoutResponse
    {
        public PayMongoCheckoutResponseData? Data { get; set; }
    }

    private class PayMongoCheckoutResponseData
    {
        public string Id { get; set; } = string.Empty;
        public PayMongoCheckoutAttributes? Attributes { get; set; }
    }

    private class PayMongoWebhookEvent
    {
        public PayMongoWebhookData? Data { get; set; }
    }

    private class PayMongoWebhookData
    {
        public string Id { get; set; } = string.Empty;
        public PayMongoWebhookAttributes? Attributes { get; set; }
    }

    private class PayMongoWebhookAttributes
    {
        public string? Type { get; set; }
        public PayMongoWebhookInnerData? Data { get; set; }
    }

    private class PayMongoWebhookInnerData
    {
        public PayMongoCheckoutAttributes? Attributes { get; set; }
    }

    #endregion
}
