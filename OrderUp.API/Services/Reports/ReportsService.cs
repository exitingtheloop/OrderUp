using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data;
using OrderUp.API.Data.Entities;
using OrderUp.Shared.Contracts.Reports;

namespace OrderUp.API.Services.Reports;

public interface IReportsService
{
    Task<SalesReportResponse> GetSalesReportAsync(DateOnly startDate, DateOnly endDate);
}

public class ReportsService : IReportsService
{
    private readonly DataContext _context;

    public ReportsService(DataContext context)
    {
        _context = context;
    }

    public async Task<SalesReportResponse> GetSalesReportAsync(DateOnly startDate, DateOnly endDate)
    {
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        // Get all orders in date range
        var orders = await _context.Orders
    .Include(o => o.Items)
         .ThenInclude(i => i.Addons)
     .Where(o => o.CreatedAtUtc >= startDateTime && o.CreatedAtUtc <= endDateTime)
        .ToListAsync();

        // Only count paid orders for revenue
        var paidOrders = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).ToList();

        var totalRevenue = paidOrders.Sum(o => CalculateOrderTotal(o));
        var totalOrders = orders.Count;
        var completedOrders = orders.Count(o => o.Status == OrderStatus.Completed);
        var cancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);
        var averageOrderValue = paidOrders.Count > 0 ? totalRevenue / paidOrders.Count : 0;

        // Daily breakdown
        var dailySales = paidOrders
            .GroupBy(o => DateOnly.FromDateTime(o.CreatedAtUtc))
            .Select(g => new DailySalesDto(
                g.Key,
                g.Sum(o => CalculateOrderTotal(o)),
                g.Count()
            ))
            .OrderByDescending(d => d.Date)
            .ToList();

        // Fill in missing days with zero
        var allDays = new List<DailySalesDto>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var existing = dailySales.FirstOrDefault(d => d.Date == date);
            allDays.Add(existing ?? new DailySalesDto(date, 0, 0));
        }
        allDays = allDays.OrderByDescending(d => d.Date).ToList();

        // Top products (from paid orders only)
        var topProducts = paidOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductNameSnapshot, i.VariantNameSnapshot })
            .Select(g => new TopProductDto(
                g.Key.ProductNameSnapshot,
                g.Key.VariantNameSnapshot,
                g.Sum(i => i.Quantity),
                g.Sum(i => CalculateItemTotal(i))
            ))
            .OrderByDescending(p => p.QuantitySold)
            .Take(10)
            .ToList();

        return new SalesReportResponse(
            totalRevenue,
            totalOrders,
            completedOrders,
            cancelledOrders,
            averageOrderValue,
            allDays,
            topProducts
        );
    }

    private static decimal CalculateOrderTotal(Order order)
    {
        return order.Items.Sum(i => CalculateItemTotal(i));
    }

    private static decimal CalculateItemTotal(OrderItem item)
    {
        var addonTotal = item.Addons.Sum(a => a.UnitPriceSnapshot * a.Quantity);
        return (item.BaseUnitPriceSnapshot + addonTotal) * item.Quantity;
    }
}
