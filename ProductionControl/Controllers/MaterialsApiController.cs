using Microsoft.AspNetCore.Mvc;
using ProductionControl.Models;
using ProductionControl.Services;

namespace ProductionControl.Controllers;

[ApiController]
[Route("api/materials")]
public class MaterialsApiController(ProductionManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Material>>> GetMaterials([FromQuery(Name = "low_stock")] bool lowStock = false)
    {
        return Ok(await service.GetMaterialsAsync(lowStock));
    }

    [HttpPost]
    public async Task<ActionResult<Material>> CreateMaterial([FromBody] AddMaterialRequest request)
    {
        return Ok(await service.CreateMaterialAsync(request));
    }

    [HttpPut("{id}/stock")]
    public async Task<ActionResult<Material>> UpdateStock(int id, [FromBody] UpdateStockRequest request)
    {
        return Ok(await service.UpdateMaterialStockAsync(id, request.Amount));
    }
}