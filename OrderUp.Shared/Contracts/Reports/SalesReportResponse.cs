namespace OrderUp.Shared.Contracts.Reports;

/// <summary>
/// Sales report response with revenue, order stats, and breakdowns.
/// </summary>
public record SalesReportResponse(
    decimal TotalRevenue,
    int TotalOrders,
    int CompletedOrders,
    int CancelledOrders,
    decimal AverageOrderValue,
    List<DailySalesDto> DailySales,
    List<TopProductDto> TopProducts
);

/// <summary>
/// Revenue and order count for a single day.
/// </summary>
public record DailySalesDto(
    DateOnly Date,
    decimal Revenue,
    int OrderCount
);

/// <summary>
/// Top selling product with quantity and revenue.
/// </summary>
public record TopProductDto(
    string ProductName,
    string VariantName,
    int QuantitySold,
    decimal Revenue
);
