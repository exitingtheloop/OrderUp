using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderUp.API.Data.Seed;
using OrderUp.API.Services.Admin;
using OrderUp.Shared.Contracts.Menu.Requests;
using OrderUp.Shared.Contracts.Menu.Responses;

namespace OrderUp.API.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = IdentitySeeder.AdminRole)]
public class AdminProductsController : ControllerBase
{
    private readonly IAdminMenuService _adminMenuService;

    public AdminProductsController(IAdminMenuService adminMenuService)
    {
        _adminMenuService = adminMenuService;
    }

    #region Products

    /// <summary>
    /// Gets all products (including unavailable) with variants and allowed addons.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAllProducts()
    {
        var products = await _adminMenuService.GetAllProductsAsync();
        return Ok(products);
    }

    /// <summary>
    /// Gets a single product with variants and allowed addons.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await _adminMenuService.GetProductAsync(id);

        if (product is null)
            return NotFound(new { error = $"Product {id} not found." });

        return Ok(product);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductRequest request)
    {
        try
        {
            var product = await _adminMenuService.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, UpdateProductRequest request)
    {
        try
        {
            var product = await _adminMenuService.UpdateProductAsync(id, request);

            if (product is null)
                return NotFound(new { error = $"Product {id} not found." });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a product.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var deleted = await _adminMenuService.DeleteProductAsync(id);

            if (!deleted)
                return NotFound(new { error = $"Product {id} not found." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Variants

    /// <summary>
    /// Gets a specific variant for a product.
    /// </summary>
    [HttpGet("{productId:int}/variants/{variantId:int}")]
    public async Task<ActionResult<ProductVariantDto>> GetVariant(int productId, int variantId)
    {
        var variant = await _adminMenuService.GetVariantAsync(productId, variantId);

        if (variant is null)
            return NotFound(new { error = $"Variant {variantId} not found for product {productId}." });

        return Ok(variant);
    }

    /// <summary>
    /// Creates a new variant for a product.
    /// </summary>
    [HttpPost("{productId:int}/variants")]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(int productId, CreateProductVariantRequest request)
    {
        try
        {
            var variant = await _adminMenuService.CreateVariantAsync(productId, request);
            return CreatedAtAction(nameof(GetVariant), new { productId, variantId = variant.Id }, variant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates a variant.
    /// </summary>
    [HttpPut("{productId:int}/variants/{variantId:int}")]
    public async Task<ActionResult<ProductVariantDto>> UpdateVariant(int productId, int variantId, UpdateProductVariantRequest request)
    {
        try
        {
            var variant = await _adminMenuService.UpdateVariantAsync(productId, variantId, request);

            if (variant is null)
                return NotFound(new { error = $"Variant {variantId} not found for product {productId}." });

            return Ok(variant);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a variant.
    /// </summary>
    [HttpDelete("{productId:int}/variants/{variantId:int}")]
    public async Task<ActionResult> DeleteVariant(int productId, int variantId)
    {
        try
        {
            var deleted = await _adminMenuService.DeleteVariantAsync(productId, variantId);

            if (!deleted)
                return NotFound(new { error = $"Variant {variantId} not found for product {productId}." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Product Addons

    /// <summary>
    /// Updates the allowed addons for a product (replaces all mappings).
    /// </summary>
    [HttpPut("{productId:int}/addons")]
    public async Task<ActionResult<ProductDto>> UpdateProductAddons(int productId, UpdateProductAddonsRequest request)
    {
        try
        {
            var product = await _adminMenuService.UpdateProductAddonsAsync(productId, request);

            if (product is null)
                return NotFound(new { error = $"Product {productId} not found." });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion
}
