using Microsoft.AspNetCore.Mvc;
using ProductionControl.Models;
using ProductionControl.Services;

namespace ProductionControl.Controllers;

[ApiController]
[Route("api/calculate")]
public class CalculateApiController(ProductionManagementService service) : ControllerBase
{
    [HttpPost("production")]
    public async Task<IActionResult> CalculateProduction([FromBody] ProductionCalculationRequest request)
    {
        var product = await service.GetProductsAsync(null, null);
        var target = product.SingleOrDefault(item => item.Id == request.ProductId);
        if (target is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            productId = request.ProductId,
            quantity = request.Quantity,
            minutes = service.CalculateProductionMinutes(target, request.Quantity, 1.0)
        });
    }
}