using OrderUp.API.Payments.Abstractions;
using OrderUp.API.Payments.Gateways;

namespace OrderUp.API.Payments;

/// <summary>
/// Extension methods for registering payment services.
/// </summary>
public static class PaymentServiceExtensions
{
    /// <summary>
    /// Adds payment services to the DI container.
    /// </summary>
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind options
        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));
        services.Configure<StripeOptions>(configuration.GetSection(StripeOptions.SectionName));
        services.Configure<PayMongoOptions>(configuration.GetSection(PayMongoOptions.SectionName));

        // Register gateways
        services.AddScoped<IPaymentGateway, StripeGateway>();

        // PayMongo needs an HttpClient
        services.AddHttpClient<IPaymentGateway, PayMongoGateway>();

        // Register resolver and façade
        services.AddScoped<IPaymentGatewayResolver, PaymentGatewayResolver>();
        services.AddScoped<IPaymentService, PaymentService>();

        return services;
    }
}
