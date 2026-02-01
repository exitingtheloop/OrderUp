using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OrderUp.API.Payments;
using OrderUp.API.Payments.Abstractions;

namespace OrderUp.Tests.Payments;

public class PaymentGatewayResolverTests
{
    private readonly Mock<IPaymentGateway> _stripeGateway;
    private readonly Mock<IPaymentGateway> _payMongoGateway;
    private readonly List<IPaymentGateway> _gateways;

    public PaymentGatewayResolverTests()
    {
        _stripeGateway = new Mock<IPaymentGateway>();
        _stripeGateway.Setup(g => g.Name).Returns("Stripe");

        _payMongoGateway = new Mock<IPaymentGateway>();
        _payMongoGateway.Setup(g => g.Name).Returns("PayMongo");

        _gateways = new List<IPaymentGateway>
        {
            _stripeGateway.Object,
            _payMongoGateway.Object
        };
    }

    [Fact]
    public void GetActiveGateway_ReturnsStripe_WhenConfiguredForStripe()
    {
        // Arrange
        var options = Options.Create(new PaymentsOptions { Provider = "Stripe" });
        var resolver = new PaymentGatewayResolver(_gateways, options);

        // Act
        var gateway = resolver.GetActiveGateway();

        // Assert
        Assert.Equal("Stripe", gateway.Name);
    }

    [Fact]
    public void GetActiveGateway_ReturnsPayMongo_WhenConfiguredForPayMongo()
    {
        // Arrange
        var options = Options.Create(new PaymentsOptions { Provider = "PayMongo" });
        var resolver = new PaymentGatewayResolver(_gateways, options);

        // Act
        var gateway = resolver.GetActiveGateway();

        // Assert
        Assert.Equal("PayMongo", gateway.Name);
    }

    [Fact]
    public void GetActiveGateway_IsCaseInsensitive()
    {
        // Arrange
        var options = Options.Create(new PaymentsOptions { Provider = "stripe" }); // lowercase
        var resolver = new PaymentGatewayResolver(_gateways, options);

        // Act
        var gateway = resolver.GetActiveGateway();

        // Assert
        Assert.Equal("Stripe", gateway.Name);
    }

    [Fact]
    public void GetActiveGateway_ThrowsException_WhenProviderNotFound()
    {
        // Arrange
        var options = Options.Create(new PaymentsOptions { Provider = "UnknownProvider" });
        var resolver = new PaymentGatewayResolver(_gateways, options);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => resolver.GetActiveGateway());
        Assert.Contains("UnknownProvider", ex.Message);
        Assert.Contains("Stripe", ex.Message); // Should list available gateways
        Assert.Contains("PayMongo", ex.Message);
    }

    [Fact]
    public void GetGateway_ReturnsGateway_WhenExists()
    {
        // Arrange
        var options = Options.Create(new PaymentsOptions { Provider = "Stripe" });
        var resolver = new PaymentGatewayResolver(_gateways, options);

        // Act
        var gateway = resolver.GetGateway("PayMongo");

        // Assert
        Assert.NotNull(gateway);
        Assert.Equal("PayMongo", gateway.Name);
    }

    [Fact]
    public void GetGateway_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var options = Options.Create(new PaymentsOptions { Provider = "Stripe" });
        var resolver = new PaymentGatewayResolver(_gateways, options);

        // Act
        var gateway = resolver.GetGateway("NonExistent");

        // Assert
        Assert.Null(gateway);
    }
}
