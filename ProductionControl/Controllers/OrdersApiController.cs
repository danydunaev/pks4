using Microsoft.AspNetCore.Mvc;
using ProductionControl.Models;
using ProductionControl.Services;

namespace ProductionControl.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersApiController(ProductionManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] string? status = null, [FromQuery] string? date = null)
    {
        return Ok(await service.GetOrdersAsync(status, date));
    }

    [HttpPost]
    public async Task<ActionResult<WorkOrder>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        return Ok(await service.CreateOrderAsync(request));
    }

    [HttpPut("{id}/progress")]
    public async Task<ActionResult<WorkOrder>> UpdateProgress(int id, [FromBody] UpdateProgressRequest request)
    {
        return Ok(await service.UpdateOrderProgressAsync(id, request.Percent));
    }

    [HttpGet("{id}/details")]
    public async Task<ActionResult<WorkOrder>> GetDetails(int id)
    {
        return Ok(await service.GetOrderDetailsAsync(id));
    }
}