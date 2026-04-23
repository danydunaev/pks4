using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;

namespace TouristGuide.Controllers;

public class AttractionsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public AttractionsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Details(int id)
    {
        var attraction = await _dbContext.Attractions
            .AsNoTracking()
            .Include(item => item.City)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (attraction is null)
        {
            return NotFound();
        }

        return View(attraction);
    }
}