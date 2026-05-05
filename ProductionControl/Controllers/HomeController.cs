using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductionControl.Models;
using ProductionControl.Services;
using ProductionControl.ViewModels;

namespace ProductionControl.Controllers;

public class HomeController(ProductionManagementService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? productCategory, string? productSearch, string? orderStatus)
    {
        var viewModel = new DashboardViewModel
        {
            ProductSearch = productSearch,
            ProductCategory = productCategory,
            OrderStatus = orderStatus,
            Materials = await service.GetMaterialsAsync(false),
            Products = await service.GetProductsAsync(productCategory, productSearch),
            Lines = await service.GetLinesAsync(false),
            Orders = await service.GetOrdersAsync(orderStatus, null)
        };

        ViewBag.Categories = await service.GetCategoriesAsync();
        ViewBag.AvailableLines = viewModel.Lines
            .Where(line => line.Status == "Active" && line.CurrentWorkOrderId is null)
            .ToList();

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMaterial(AddMaterialRequest request)
    {
        if (ModelState.IsValid)
        {
            await service.CreateMaterialAsync(request);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplenishMaterial(int id, decimal amount)
    {
        if (amount > 0)
        {
            await service.UpdateMaterialStockAsync(id, amount);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request)
    {
        if (ModelState.IsValid)
        {
            var availableMaterials = await service.GetMaterialsAsync(false);
            var materialsText = !string.IsNullOrWhiteSpace(request.MaterialsText)
                ? request.MaterialsText
                : request.Specifications;

            request.Materials = ParseMaterials(materialsText, availableMaterials);
            await service.CreateProductAsync(request);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await service.CreateOrderAsync(request);
            }
            catch (InvalidOperationException ex)
            {
                // Show a friendly error on the dashboard instead of throwing 500
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartOrder(int id)
    {
        try
        {
            await service.SetOrderStatusAsync(id, "InProgress");
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        await service.SetOrderStatusAsync(id, "Cancelled");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLineStatus(int id, string status, double efficiencyFactor, int? displayedProgress)
    {
        await service.UpdateLineStatusAsync(id, status, displayedProgress);
        await service.UpdateLineEfficiencyAsync(id, efficiencyFactor);
        return RedirectToAction(nameof(Index));
    }

    private static List<ProductMaterialRequest> ParseMaterials(string? specificationText, IEnumerable<Material> availableMaterials)
    {
        if (string.IsNullOrWhiteSpace(specificationText))
        {
            return [];
        }

        var assignments = new List<ProductMaterialRequest>();
        var materialsByName = availableMaterials
            .GroupBy(material => material.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var parts = specificationText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var tokens = part.Split([':', '=', 'x', 'X'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
            {
                continue;
            }

            var materialToken = tokens[0];
            var materialId = 0;

            if (!int.TryParse(materialToken, out materialId))
            {
                if (!materialsByName.TryGetValue(materialToken, out var matchedMaterial))
                {
                    continue;
                }

                materialId = matchedMaterial.Id;
            }

            if (materialId > 0 && decimal.TryParse(tokens[1], out var quantityNeeded))
            {
                assignments.Add(new ProductMaterialRequest
                {
                    MaterialId = materialId,
                    QuantityNeeded = quantityNeeded
                });
            }
        }

        return assignments;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
