using Microsoft.Extensions.Options;

namespace OrderUp.API.Payments.Abstractions;

/// <summary>
/// Resolves the active payment gateway based on configuration.
/// </summary>
public class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly PaymentsOptions _options;

    public PaymentGatewayResolver(
        IEnumerable<IPaymentGateway> gateways,
        IOptions<PaymentsOptions> options)
    {
        _gateways = gateways;
        _options = options.Value;
    }

    public IPaymentGateway GetActiveGateway()
    {
        var gateway = GetGateway(_options.Provider);

        if (gateway is null)
        {
            throw new InvalidOperationException(
                $"No payment gateway found for provider '{_options.Provider}'. " +
                $"Available gateways: {string.Join(", ", _gateways.Select(g => g.Name))}");
        }

        return gateway;
    }

    public IPaymentGateway? GetGateway(string name)
    {
        return _gateways.FirstOrDefault(g =>
        g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
