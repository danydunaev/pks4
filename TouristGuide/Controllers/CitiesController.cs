using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;

namespace TouristGuide.Controllers;

public class CitiesController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public CitiesController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var cities = await _dbContext.Cities
            .AsNoTracking()
            .OrderBy(city => city.Name)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            cities = cities
                .Where(city => city.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewData["Search"] = search;

        return View(cities);
    }

    public async Task<IActionResult> Details(int id)
    {
        var city = await _dbContext.Cities
            .AsNoTracking()
            .Include(item => item.Attractions)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (city is null)
        {
            return NotFound();
        }

        return View(city);
    }
}