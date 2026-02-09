using Microsoft.AspNetCore.Mvc;
using OrderUp.Shared.Contracts;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public SettingsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public ActionResult<AppSettingsResponse> GetSettings()
    {
        var currencySymbol = _configuration["AppSettings:CurrencySymbol"] ?? "₱";
        var currencyCode = _configuration["AppSettings:CurrencyCode"] ?? "PHP";
        var cafeName = _configuration["AppSettings:CafeName"] ?? "OrderUp Cafe";
        var receiptFooter = _configuration["AppSettings:ReceiptFooter"] ?? "Thank you for your order!";

        return Ok(new AppSettingsResponse(currencySymbol, currencyCode, cafeName, receiptFooter));
    }
}
