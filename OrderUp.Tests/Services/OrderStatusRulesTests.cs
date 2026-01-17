using OrderUp.API.Services.Orders;
using OrderUp.Shared.Enums;

namespace OrderUp.Tests.Services;

public class OrderStatusRulesTests
{
    #region Valid Transitions

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Ready)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Ready, OrderStatus.Completed)]
    public void IsValidTransition_AllowedTransition_ReturnsTrue(OrderStatus from, OrderStatus to)
    {
        // Act
        var result = OrderStatusRules.IsValidTransition(from, to);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void IsValidTransition_SameStatus_ReturnsTrue(OrderStatus status)
    {
        // Act
        var result = OrderStatusRules.IsValidTransition(status, status);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region Invalid Transitions

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Pending, OrderStatus.Ready)]
    [InlineData(OrderStatus.Pending, OrderStatus.Completed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Ready)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Pending)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Completed)]
    [InlineData(OrderStatus.Ready, OrderStatus.Pending)]
    [InlineData(OrderStatus.Ready, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Ready, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Ready, OrderStatus.Cancelled)]
    public void IsValidTransition_DisallowedTransition_ReturnsFalse(OrderStatus from, OrderStatus to)
    {
        // Act
        var result = OrderStatusRules.IsValidTransition(from, to);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(OrderStatus.Completed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Completed, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Completed, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Completed, OrderStatus.Ready)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled)]
    public void IsValidTransition_FromCompleted_ReturnsFalse(OrderStatus from, OrderStatus to)
    {
        // Act
        var result = OrderStatusRules.IsValidTransition(from, to);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Ready)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Completed)]
    public void IsValidTransition_FromCancelled_ReturnsFalse(OrderStatus from, OrderStatus to)
    {
        // Act
        var result = OrderStatusRules.IsValidTransition(from, to);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ValidateTransition

    [Fact]
    public void ValidateTransition_ValidTransition_DoesNotThrow()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
            OrderStatusRules.ValidateTransition(OrderStatus.Pending, OrderStatus.Confirmed));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateTransition_InvalidTransition_ThrowsWithMessage()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            OrderStatusRules.ValidateTransition(OrderStatus.Pending, OrderStatus.Completed));

        Assert.Contains("Cannot transition order from 'Pending' to 'Completed'", exception.Message);
        Assert.Contains("Allowed transitions from 'Pending'", exception.Message);
    }

    [Fact]
    public void ValidateTransition_FromTerminalState_ThrowsNoTransitionsMessage()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            OrderStatusRules.ValidateTransition(OrderStatus.Completed, OrderStatus.Pending));

        Assert.Contains("No transitions allowed from 'Completed'", exception.Message);
    }

    #endregion

    #region GetAllowedTransitions

    [Fact]
    public void GetAllowedTransitions_Pending_ReturnsConfirmedAndCancelled()
    {
        // Act
        var allowed = OrderStatusRules.GetAllowedTransitions(OrderStatus.Pending);

        // Assert
        Assert.Equal(2, allowed.Length);
        Assert.Contains(OrderStatus.Confirmed, allowed);
        Assert.Contains(OrderStatus.Cancelled, allowed);
    }

    [Fact]
    public void GetAllowedTransitions_Completed_ReturnsEmpty()
    {
        // Act
        var allowed = OrderStatusRules.GetAllowedTransitions(OrderStatus.Completed);

        // Assert
        Assert.Empty(allowed);
    }

    [Fact]
    public void GetAllowedTransitions_Cancelled_ReturnsEmpty()
    {
        // Act
        var allowed = OrderStatusRules.GetAllowedTransitions(OrderStatus.Cancelled);

        // Assert
        Assert.Empty(allowed);
    }

    #endregion
}
