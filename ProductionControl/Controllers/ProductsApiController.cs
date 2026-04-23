using Microsoft.AspNetCore.Mvc;
using ProductionControl.Models;
using ProductionControl.Services;

namespace ProductionControl.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsApiController(ProductionManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? category = null)
    {
        return Ok(await service.GetProductsAsync(category, null));
    }

    [HttpGet("{id}/materials")]
    public async Task<IActionResult> GetProductMaterials(int id)
    {
        return Ok(await service.GetProductMaterialsAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        return Ok(await service.CreateProductAsync(request));
    }
}