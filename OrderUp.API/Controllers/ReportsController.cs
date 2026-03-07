using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Services.Reports;
using OrderUp.Shared.Contracts.Reports;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    /// <summary>
    /// Gets sales report for the specified date range.
    /// </summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportResponse>> GetSalesReport(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        // Default to today if not specified
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? end;

        if (start > end)
        {
            return BadRequest("Start date cannot be after end date.");
        }

        var report = await _reportsService.GetSalesReportAsync(start, end);
        return Ok(report);
    }
}
