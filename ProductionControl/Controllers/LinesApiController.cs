using Microsoft.AspNetCore.Mvc;
using ProductionControl.Models;
using ProductionControl.Services;

namespace ProductionControl.Controllers;

[ApiController]
[Route("api/lines")]
public class LinesApiController(ProductionManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetLines([FromQuery(Name = "available")] bool available = false)
    {
        return Ok(await service.GetLinesAsync(available));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] SetLineStatusRequest request)
    {
        return Ok(await service.UpdateLineStatusAsync(id, request.Status));
    }

    [HttpPut("{id}/efficiency")]
    public async Task<IActionResult> UpdateEfficiency(int id, [FromBody] SetLineEfficiencyRequest request)
    {
        return Ok(await service.UpdateLineEfficiencyAsync(id, request.Factor));
    }

    [HttpGet("{id}/schedule")]
    public async Task<IActionResult> GetSchedule(int id)
    {
        return Ok(await service.GetLineScheduleAsync(id));
    }
}