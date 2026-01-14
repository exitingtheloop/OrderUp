using Microsoft.EntityFrameworkCore;
using OrderUp.API.Data;
using OrderUp.API.Mapping;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Services.Menu;

public class MenuService : IMenuService
{
    private readonly DataContext _context;

    public MenuService(DataContext context)
    {
        _context = context;
    }

    public async Task<MenuDto> GetMenuAsync()
    {
        var categories = await _context.ProductCategories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => c.ToDto())
            .ToListAsync();

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsAvailable))
            .Include(p => p.ProductAddons)
                .ThenInclude(pa => pa.Addon)
            .Where(p => p.IsAvailable)
            .Select(p => p.ToDto())
            .ToListAsync();

        return new MenuDto(categories, products);
    }
}
