using ProductionControl.Models;

namespace ProductionControl.Data;

public static class ProductionSeedData
{
    public static async Task InitializeAsync(ApplicationDbContext dbContext)
    {
        if (dbContext.Products.Any())
        {
            return;
        }

        var materials = new List<Material>
        {
            new() { Name = "Стальной лист", Quantity = 420, UnitOfMeasure = "кг", MinimalStock = 120 },
            new() { Name = "Пластиковая гранула", Quantity = 860, UnitOfMeasure = "кг", MinimalStock = 200 },
            new() { Name = "Промышленная краска", Quantity = 145, UnitOfMeasure = "л", MinimalStock = 50 },
            new() { Name = "Крепеж", Quantity = 6000, UnitOfMeasure = "шт", MinimalStock = 1500 }
        };

        var products = new List<Product>
        {
            new()
            {
                Name = "Шкаф управления",
                Description = "Промышленный шкаф для систем автоматизации.",
                Specifications = "{\"width\":\"800mm\",\"height\":\"2000mm\",\"protection\":\"IP54\"}",
                Category = "Электрика",
                MinimalStock = 12,
                ProductionTimePerUnit = 90
            },
            new()
            {
                Name = "Корпус насоса",
                Description = "Защитный корпус для насосных узлов.",
                Specifications = "{\"material\":\"steel\",\"finish\":\"powder-coat\"}",
                Category = "Механика",
                MinimalStock = 20,
                ProductionTimePerUnit = 55
            },
            new()
            {
                Name = "Сенсорный модуль",
                Description = "Компактный модуль для электроники мониторинга.",
                Specifications = "{\"channels\":4,\"voltage\":\"24V\"}",
                Category = "Электроника",
                MinimalStock = 35,
                ProductionTimePerUnit = 25
            }
        };

        var lines = new List<ProductionLine>
        {
            new() { Name = "Сборочная линия A", Status = "Active", EfficiencyFactor = 1.0 },
            new() { Name = "Сборочная линия B", Status = "Active", EfficiencyFactor = 1.2 },
            new() { Name = "Покрасочная линия", Status = "Stopped", EfficiencyFactor = 0.8 }
        };

        dbContext.Materials.AddRange(materials);
        dbContext.Products.AddRange(products);
        dbContext.ProductionLines.AddRange(lines);
        await dbContext.SaveChangesAsync();

        var steel = dbContext.Materials.Single(material => material.Name == "Стальной лист");
        var plastic = dbContext.Materials.Single(material => material.Name == "Пластиковая гранула");
        var paint = dbContext.Materials.Single(material => material.Name == "Промышленная краска");
        var fasteners = dbContext.Materials.Single(material => material.Name == "Крепеж");

        var cabinet = dbContext.Products.Single(product => product.Name == "Шкаф управления");
        var pump = dbContext.Products.Single(product => product.Name == "Корпус насоса");
        var sensor = dbContext.Products.Single(product => product.Name == "Сенсорный модуль");

        dbContext.ProductMaterials.AddRange(
            new ProductMaterial { ProductId = cabinet.Id, MaterialId = steel.Id, QuantityNeeded = 18 },
            new ProductMaterial { ProductId = cabinet.Id, MaterialId = fasteners.Id, QuantityNeeded = 120 },
            new ProductMaterial { ProductId = cabinet.Id, MaterialId = paint.Id, QuantityNeeded = 2 },
            new ProductMaterial { ProductId = pump.Id, MaterialId = steel.Id, QuantityNeeded = 8 },
            new ProductMaterial { ProductId = pump.Id, MaterialId = fasteners.Id, QuantityNeeded = 40 },
            new ProductMaterial { ProductId = sensor.Id, MaterialId = plastic.Id, QuantityNeeded = 4 },
            new ProductMaterial { ProductId = sensor.Id, MaterialId = fasteners.Id, QuantityNeeded = 10 }
        );

        await dbContext.SaveChangesAsync();

        var lineA = dbContext.ProductionLines.Single(line => line.Name == "Сборочная линия A");
        var lineB = dbContext.ProductionLines.Single(line => line.Name == "Сборочная линия B");

        var cabinetOrder = new WorkOrder
        {
            ProductId = cabinet.Id,
            ProductionLineId = lineA.Id,
            Quantity = 8,
            StartDate = DateTime.Now.AddHours(-4),
            EstimatedEndDate = DateTime.Now.AddHours(8),
            Status = "InProgress",
            ProgressPercent = 45
        };

        var sensorOrder = new WorkOrder
        {
            ProductId = sensor.Id,
            ProductionLineId = lineB.Id,
            Quantity = 14,
            StartDate = DateTime.Now.AddHours(-1),
            EstimatedEndDate = DateTime.Now.AddHours(3),
            Status = "Pending",
            ProgressPercent = 0
        };

        dbContext.WorkOrders.AddRange(cabinetOrder, sensorOrder);
        await dbContext.SaveChangesAsync();

        lineA.CurrentWorkOrderId = cabinetOrder.Id;
        lineB.CurrentWorkOrderId = sensorOrder.Id;
        await dbContext.SaveChangesAsync();
    }
}