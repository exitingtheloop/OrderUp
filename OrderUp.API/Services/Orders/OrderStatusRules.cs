using OrderUp.Shared.Enums;

namespace OrderUp.API.Services.Orders;

/// <summary>
/// Defines and validates allowed order status transitions.
/// </summary>
public static class OrderStatusRules
{
    /// <summary>
    /// Allowed transitions: CurrentStatus -> Set of valid next statuses.
    /// </summary>
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> AllowedTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.Ready, OrderStatus.Cancelled],
        [OrderStatus.Ready] = [OrderStatus.Completed],
        [OrderStatus.Completed] = [],
        [OrderStatus.Cancelled] = []
    };

    /// <summary>
    /// Checks if transitioning from currentStatus to newStatus is allowed.
    /// </summary>
    public static bool IsValidTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        // Same status is always allowed (no-op)
        if (currentStatus == newStatus)
            return true;

        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);
    }

    /// <summary>
    /// Validates the transition and throws InvalidOperationException if not allowed.
    /// </summary>
    public static void ValidateTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        if (!IsValidTransition(currentStatus, newStatus))
        {
            var allowedList = GetAllowedTransitions(currentStatus);
            var allowedMessage = allowedList.Length > 0
                 ? $"Allowed transitions from '{currentStatus}': {string.Join(", ", allowedList)}."
                 : $"No transitions allowed from '{currentStatus}'.";

            throw new InvalidOperationException($"Cannot transition order from '{currentStatus}' " +
                $"to '{newStatus}'. {allowedMessage}");
        }
    }

    /// <summary>
    /// Gets the list of allowed next statuses for a given current status.
    /// </summary>
    public static OrderStatus[] GetAllowedTransitions(OrderStatus currentStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) ? [.. allowed] : [];
    }
}
