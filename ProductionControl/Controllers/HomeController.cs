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
            Orders = await service.GetOrdersAsync(orderStatus, null),
            Lines = await service.GetLinesAsync(false)
        };

        ViewBag.Categories = await service.GetCategoriesAsync();
        ViewBag.AvailableLines = await service.GetLinesAsync(true);

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
            request.Materials = ParseMaterials(request.MaterialsText ?? request.Specifications);
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
            await service.CreateOrderAsync(request);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartOrder(int id)
    {
        await service.SetOrderStatusAsync(id, "InProgress");
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
    public async Task<IActionResult> UpdateLineStatus(int id, string status, double efficiencyFactor)
    {
        await service.UpdateLineStatusAsync(id, status);
        await service.UpdateLineEfficiencyAsync(id, efficiencyFactor);
        return RedirectToAction(nameof(Index));
    }

    private static List<ProductMaterialRequest> ParseMaterials(string? specificationText)
    {
        if (string.IsNullOrWhiteSpace(specificationText))
        {
            return [];
        }

        var assignments = new List<ProductMaterialRequest>();
        var parts = specificationText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var tokens = part.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 2)
            {
                continue;
            }

            if (int.TryParse(tokens[0], out var materialId) && decimal.TryParse(tokens[1], out var quantityNeeded))
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
