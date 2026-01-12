using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Services.Menu;

public interface IMenuService
{
    Task<MenuDto> GetMenuAsync();
}
