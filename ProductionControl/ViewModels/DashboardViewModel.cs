using ProductionControl.Models;

namespace ProductionControl.ViewModels;

public class DashboardViewModel
{
    public string? ProductSearch { get; set; }

    public string? ProductCategory { get; set; }

    public string? OrderStatus { get; set; }

    public List<Material> Materials { get; set; } = [];

    public List<Product> Products { get; set; } = [];

    public List<WorkOrder> Orders { get; set; } = [];

    public List<ProductionLine> Lines { get; set; } = [];

    public int MaterialCount => Materials.Count;

    public int LowStockMaterialCount => Materials.Count(material => material.Quantity <= material.MinimalStock);

    public int ActiveOrderCount => Orders.Count(order => order.Status is "Pending" or "InProgress");

    public int RunningLineCount => Lines.Count(line => line.Status == "Active");
}