using Microsoft.EntityFrameworkCore;
using ProductionControl.Data;
using ProductionControl.Models;
using System.Collections.Concurrent;

namespace ProductionControl.Services;

public class ProductionManagementService(ApplicationDbContext dbContext)
{
    private static readonly ConcurrentDictionary<int, DateTime> PausedSinceByLineId = new();

    private async Task RefreshOrderProgressAsync()
    {
        var now = DateTime.Now;
        var orders = await dbContext.WorkOrders
            .Include(order => order.ProductionLine)
            .Where(order => order.Status == "InProgress" || (order.Status == "Pending" && order.ProductionLineId != null))
            .ToListAsync();

        var hasChanges = false;

        foreach (var order in orders)
        {
            var hasAssignedAndActiveLine = order.ProductionLine is not null
                && order.ProductionLine.Status == "Active"
                && order.ProductionLine.CurrentWorkOrderId == order.Id;

            if (order.Status == "Pending" && !hasAssignedAndActiveLine)
            {
                continue;
            }

            if (order.Status == "Pending" && hasAssignedAndActiveLine)
            {
                order.Status = "InProgress";
                if (order.StartDate > now)
                {
                    order.StartDate = now;
                }

                hasChanges = true;
            }

            if (order.ProductionLine is not null
                && order.ProductionLine.CurrentWorkOrderId == order.Id
                && order.ProductionLine.Status != "Active")
            {
                continue;
            }

            var totalMinutes = Math.Max(1.0, (order.EstimatedEndDate - order.StartDate).TotalMinutes);
            var elapsedMinutes = Math.Max(0.0, (now - order.StartDate).TotalMinutes);
            var calculatedPercent = (int)Math.Clamp(Math.Floor((elapsedMinutes / totalMinutes) * 100.0), 0, 100);
            if (elapsedMinutes > 0 && calculatedPercent == 0)
            {
                calculatedPercent = 1;
            }
            var newPercent = Math.Max(order.ProgressPercent, calculatedPercent);

            if (newPercent != order.ProgressPercent)
            {
                order.ProgressPercent = newPercent;
                hasChanges = true;
            }

            if (order.ProgressPercent >= 100 && order.Status != "Completed")
            {
                order.ProgressPercent = 100;
                order.Status = "Completed";
                if (order.ProductionLine is not null && order.ProductionLine.CurrentWorkOrderId == order.Id)
                {
                    order.ProductionLine.CurrentWorkOrderId = null;
                }

                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<Material>> GetMaterialsAsync(bool lowStockOnly)
    {
        var query = dbContext.Materials.AsQueryable();
        if (lowStockOnly)
        {
            query = query.Where(material => material.Quantity <= material.MinimalStock);
        }

        return await query.OrderBy(material => material.Name).ToListAsync();
    }

    public async Task<Material> CreateMaterialAsync(AddMaterialRequest request)
    {
        var material = new Material
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            UnitOfMeasure = request.UnitOfMeasure.Trim(),
            MinimalStock = request.MinimalStock
        };

        dbContext.Materials.Add(material);
        await dbContext.SaveChangesAsync();
        return material;
    }

    public async Task<Material> UpdateMaterialStockAsync(int id, decimal amount)
    {
        var material = await dbContext.Materials.SingleAsync(entity => entity.Id == id);
        material.Quantity += amount;
        await dbContext.SaveChangesAsync();
        return material;
    }

    public async Task<List<Product>> GetProductsAsync(string? category, string? search)
    {
        var query = dbContext.Products
            .Include(product => product.ProductMaterials)
            .ThenInclude(productMaterial => productMaterial.Material)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(product => product.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product => product.Name.Contains(search));
        }

        return await query.OrderBy(product => product.Name).ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        return await dbContext.Products
            .Select(product => product.Category)
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync();
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Specifications = request.Specifications?.Trim(),
            Category = request.Category.Trim(),
            MinimalStock = request.MinimalStock,
            ProductionTimePerUnit = request.ProductionTimePerUnit
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        foreach (var item in request.Materials.Where(material => material.MaterialId > 0 && material.QuantityNeeded > 0))
        {
            dbContext.ProductMaterials.Add(new ProductMaterial
            {
                ProductId = product.Id,
                MaterialId = item.MaterialId,
                QuantityNeeded = item.QuantityNeeded
            });
        }

        await dbContext.SaveChangesAsync();
        return product;
    }

    public async Task<List<Material>> GetProductMaterialsAsync(int productId)
    {
        return await dbContext.ProductMaterials
            .Where(productMaterial => productMaterial.ProductId == productId)
            .Include(productMaterial => productMaterial.Material)
            .Select(productMaterial => productMaterial.Material!)
            .OrderBy(material => material.Name)
            .ToListAsync();
    }

    public async Task<List<ProductionLine>> GetLinesAsync(bool available)
    {
        await RefreshOrderProgressAsync();

        var query = dbContext.ProductionLines
            .Include(line => line.CurrentWorkOrder)
            .ThenInclude(workOrder => workOrder!.Product)
            .Include(line => line.WorkOrders)
            .ThenInclude(workOrder => workOrder.Product)
            .AsQueryable();

        if (available)
        {
            query = query.Where(line => line.Status == "Active" && line.CurrentWorkOrderId == null);
        }

        return await query.OrderBy(line => line.Name).ToListAsync();
    }

    public async Task<ProductionLine> UpdateLineStatusAsync(int id, string status)
    {
        var line = await dbContext.ProductionLines.SingleAsync(entity => entity.Id == id);
        await RefreshOrderProgressAsync();

        var currentWorkOrder = await dbContext.WorkOrders
            .SingleOrDefaultAsync(order => order.Id == line.CurrentWorkOrderId);

        var wasStopped = line.Status == "Stopped";
        var pauseStartedAt = PausedSinceByLineId.TryRemove(line.Id, out var storedPauseStart)
            ? storedPauseStart
            : (DateTime?)null;

        line.Status = status;

        if (currentWorkOrder is not null)
        {
            if (status == "Stopped")
            {
                var now = DateTime.Now;
                var totalMinutes = Math.Max(1.0, (currentWorkOrder.EstimatedEndDate - currentWorkOrder.StartDate).TotalMinutes);
                var elapsedMinutes = Math.Max(0.0, (now - currentWorkOrder.StartDate).TotalMinutes);
                var calculatedPercent = (int)Math.Clamp(Math.Floor((elapsedMinutes / totalMinutes) * 100.0), 0, 100);
                if (elapsedMinutes > 0 && calculatedPercent == 0)
                {
                    calculatedPercent = 1;
                }

                currentWorkOrder.ProgressPercent = Math.Max(currentWorkOrder.ProgressPercent, calculatedPercent);
                PausedSinceByLineId[line.Id] = DateTime.Now;
            }
            else if (status == "Active" && wasStopped && pauseStartedAt is not null)
            {
                var pauseDuration = DateTime.Now - pauseStartedAt.Value;
                currentWorkOrder.StartDate = currentWorkOrder.StartDate.Add(pauseDuration);
                currentWorkOrder.EstimatedEndDate = currentWorkOrder.EstimatedEndDate.Add(pauseDuration);
                currentWorkOrder.Status = "InProgress";
                currentWorkOrder.ProgressPercent = Math.Max(currentWorkOrder.ProgressPercent, 1);
            }
        }

        await dbContext.SaveChangesAsync();
        return line;
    }

    public async Task<ProductionLine> UpdateLineEfficiencyAsync(int id, double factor)
    {
        var line = await dbContext.ProductionLines
            .Include(entity => entity.CurrentWorkOrder)
            .ThenInclude(workOrder => workOrder!.Product)
            .SingleAsync(entity => entity.Id == id);

        line.EfficiencyFactor = Math.Clamp(factor, 0.5, 2.0);

        var currentWorkOrder = line.CurrentWorkOrder;
        if (currentWorkOrder is not null
            && (currentWorkOrder.Status == "Pending" || currentWorkOrder.Status == "InProgress")
            && currentWorkOrder.Product is not null)
        {
            var now = DateTime.Now;
            var totalMinutes = Math.Max(1.0, (currentWorkOrder.Product.ProductionTimePerUnit * currentWorkOrder.Quantity) / line.EfficiencyFactor);
            var elapsedMinutes = Math.Max(0.0, (now - currentWorkOrder.StartDate).TotalMinutes);
            var currentPercent = currentWorkOrder.Status == "InProgress"
                ? (int)Math.Clamp(Math.Floor((elapsedMinutes / Math.Max(1.0, (currentWorkOrder.EstimatedEndDate - currentWorkOrder.StartDate).TotalMinutes)) * 100.0), 0, 100)
                : currentWorkOrder.ProgressPercent;

            if (elapsedMinutes > 0 && currentPercent == 0)
            {
                currentPercent = 1;
            }

            currentWorkOrder.ProgressPercent = Math.Max(currentWorkOrder.ProgressPercent, currentPercent);
            currentWorkOrder.StartDate = now.AddMinutes(-(totalMinutes * currentPercent / 100.0));
            currentWorkOrder.EstimatedEndDate = currentWorkOrder.StartDate.AddMinutes(totalMinutes);
        }

        await dbContext.SaveChangesAsync();
        return line;
    }

    public async Task<List<WorkOrder>> GetOrdersAsync(string? status, string? date)
    {
        await RefreshOrderProgressAsync();

        var query = dbContext.WorkOrders
            .Include(order => order.Product)
            .Include(order => order.ProductionLine)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "active")
        {
            query = query.Where(order => order.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(status) && status == "active")
        {
            query = query.Where(order => order.Status == "Pending" || order.Status == "InProgress");
        }

        if (!string.IsNullOrWhiteSpace(date) && date.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.Today;
            query = query.Where(order => order.StartDate.Date == today || order.EstimatedEndDate.Date == today);
        }

        return await query.OrderByDescending(order => order.Id).ToListAsync();
    }

    public async Task<WorkOrder> CreateOrderAsync(CreateOrderRequest request)
    {
        var product = await dbContext.Products
            .Include(entity => entity.ProductMaterials)
            .ThenInclude(productMaterial => productMaterial.Material)
            .SingleAsync(entity => entity.Id == request.ProductId);

        var materialsToReserve = product.ProductMaterials.ToList();
        foreach (var materialRequirement in materialsToReserve)
        {
            var requiredQuantity = materialRequirement.QuantityNeeded * request.Quantity;
            if (materialRequirement.Material is null || materialRequirement.Material.Quantity < requiredQuantity)
            {
                throw new InvalidOperationException($"Not enough material: {materialRequirement.Material?.Name}");
            }
        }

        ProductionLine? line = null;
        if (request.ProductionLineId.HasValue)
        {
            line = await dbContext.ProductionLines.SingleAsync(entity => entity.Id == request.ProductionLineId.Value);
            if (line.Status != "Active" || line.CurrentWorkOrderId is not null)
            {
                throw new InvalidOperationException("Selected line is not available.");
            }
        }

        foreach (var materialRequirement in materialsToReserve)
        {
            var requiredQuantity = materialRequirement.QuantityNeeded * request.Quantity;
            materialRequirement.Material!.Quantity -= requiredQuantity;
        }

        var efficiencyFactor = line?.EfficiencyFactor ?? 1.0;
        var productionMinutes = CalculateProductionMinutes(product, request.Quantity, efficiencyFactor);
        var startDate = DateTime.Now;
        var order = new WorkOrder
        {
            ProductId = product.Id,
            ProductionLineId = line?.Id,
            Quantity = request.Quantity,
            StartDate = startDate,
            EstimatedEndDate = startDate.AddMinutes(productionMinutes),
            Status = line is null ? "Pending" : "InProgress",
            ProgressPercent = 0
        };

        dbContext.WorkOrders.Add(order);
        await dbContext.SaveChangesAsync();

        if (line is not null)
        {
            line.CurrentWorkOrderId = order.Id;
            await dbContext.SaveChangesAsync();
        }

        return order;
    }

    public async Task<WorkOrder> UpdateOrderProgressAsync(int id, int percent)
    {
        var order = await dbContext.WorkOrders.Include(entity => entity.ProductionLine).SingleAsync(entity => entity.Id == id);
        order.ProgressPercent = Math.Clamp(percent, 0, 100);
        order.Status = order.ProgressPercent switch
        {
            0 => order.Status == "Pending" ? "Pending" : "InProgress",
            100 => "Completed",
            _ => "InProgress"
        };

        if (order.Status == "Completed" && order.ProductionLine is not null)
        {
            order.ProductionLine.CurrentWorkOrderId = null;
        }

        await dbContext.SaveChangesAsync();
        return order;
    }

    public async Task<WorkOrder> SetOrderStatusAsync(int id, string status)
    {
        var order = await dbContext.WorkOrders
            .Include(entity => entity.ProductionLine)
            .Include(entity => entity.Product)
            .ThenInclude(product => product!.ProductMaterials)
            .ThenInclude(productMaterial => productMaterial.Material)
            .SingleAsync(entity => entity.Id == id);

        if (order.Status == status)
        {
            return order;
        }

        order.Status = status;

        if (status == "Cancelled")
        {
            ReturnUnusedMaterialsAsync(order);

            if (order.ProductionLine is not null)
            {
                order.ProductionLine.CurrentWorkOrderId = null;
            }
        }
        else if (status == "Completed")
        {
            order.ProgressPercent = status == "Completed" ? 100 : order.ProgressPercent;
            if (order.ProductionLine is not null)
            {
                order.ProductionLine.CurrentWorkOrderId = null;
            }
        }
        else if (status == "InProgress")
        {
            order.ProgressPercent = Math.Max(order.ProgressPercent, 1);
        }

        await dbContext.SaveChangesAsync();
        return order;
    }

    private void ReturnUnusedMaterialsAsync(WorkOrder order)
    {
        if (order.Product is null || order.Product.ProductMaterials.Count == 0 || order.ProgressPercent >= 100)
        {
            return;
        }

        var unusedFraction = Math.Clamp((100 - order.ProgressPercent) / 100m, 0m, 1m);
        if (unusedFraction <= 0m)
        {
            return;
        }

        foreach (var productMaterial in order.Product.ProductMaterials)
        {
            if (productMaterial.Material is null)
            {
                continue;
            }

            var reservedQuantity = productMaterial.QuantityNeeded * order.Quantity;
            var returnQuantity = reservedQuantity * unusedFraction;
            if (returnQuantity > 0m)
            {
                productMaterial.Material.Quantity += returnQuantity;
            }
        }
    }

    public async Task<WorkOrder> GetOrderDetailsAsync(int id)
    {
        await RefreshOrderProgressAsync();

        return await dbContext.WorkOrders
            .Include(order => order.Product)
            .ThenInclude(product => product!.ProductMaterials)
            .ThenInclude(productMaterial => productMaterial.Material)
            .Include(order => order.ProductionLine)
            .SingleAsync(order => order.Id == id);
    }

    public async Task<List<WorkOrder>> GetLineScheduleAsync(int lineId)
    {
        await RefreshOrderProgressAsync();

        return await dbContext.WorkOrders
            .Include(order => order.Product)
            .Where(order => order.ProductionLineId == lineId)
            .OrderBy(order => order.StartDate)
            .ToListAsync();
    }

    public double CalculateProductionMinutes(Product product, int quantity, double efficiencyFactor)
    {
        var factor = Math.Clamp(efficiencyFactor, 0.5, 2.0);
        return (quantity * product.ProductionTimePerUnit) / factor;
    }
}