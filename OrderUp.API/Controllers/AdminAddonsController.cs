using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Data.Seed;
using OrderUp.API.Services.Admin;
using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/admin/addons")]
[Authorize(Roles = IdentitySeeder.AdminRole)]
public class AdminAddonsController : ControllerBase
{
    private readonly IAdminMenuService _adminMenuService;

    public AdminAddonsController(IAdminMenuService adminMenuService)
    {
        _adminMenuService = adminMenuService;
    }

    /// <summary>
    /// Gets all addons (for selection UI).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AddonDto>>> GetAllAddons()
    {
        var addons = await _adminMenuService.GetAllAddonsAsync();
        return Ok(addons);
    }

    /// <summary>
    /// Gets a single addon.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AddonDto>> GetAddon(int id)
    {
        var addon = await _adminMenuService.GetAddonAsync(id);

        if (addon is null)
            return NotFound(new { error = $"Addon {id} not found." });

        return Ok(addon);
    }

    /// <summary>
    /// Creates a new addon.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AddonDto>> CreateAddon(CreateAddonRequest request)
    {
        try
        {
            var addon = await _adminMenuService.CreateAddonAsync(request);
            return CreatedAtAction(nameof(GetAddon), new { id = addon.Id }, addon);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing addon.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AddonDto>> UpdateAddon(int id, UpdateAddonRequest request)
    {
        try
        {
            var addon = await _adminMenuService.UpdateAddonAsync(id, request);

            if (addon is null)
                return NotFound(new { error = $"Addon {id} not found." });

            return Ok(addon);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an addon.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteAddon(int id)
    {
        try
        {
            var deleted = await _adminMenuService.DeleteAddonAsync(id);

            if (!deleted)
                return NotFound(new { error = $"Addon {id} not found." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
