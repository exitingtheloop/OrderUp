using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Services.Menu;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet]
    public async Task<ActionResult<MenuDto>> GetMenu()
    {
        var menu = await _menuService.GetMenuAsync();
        return Ok(menu);
    }
}
