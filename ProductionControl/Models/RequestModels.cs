using System.Text.Json.Serialization;

namespace ProductionControl.Models;

public class AddMaterialRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [JsonPropertyName("min_stock")]
    public decimal MinimalStock { get; set; }
}

public class UpdateStockRequest
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public class CreateProductRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("specifications")]
    public string? Specifications { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("minimal_stock")]
    public int MinimalStock { get; set; }

    [JsonPropertyName("prod_time")]
    public int ProductionTimePerUnit { get; set; }

    [JsonPropertyName("materials")]
    public List<ProductMaterialRequest> Materials { get; set; } = [];

    public string? MaterialsText { get; set; }
}

public class ProductMaterialRequest
{
    [JsonPropertyName("material_id")]
    public int MaterialId { get; set; }

    [JsonPropertyName("quantity_needed")]
    public decimal QuantityNeeded { get; set; }
}

public class CreateOrderRequest
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("line_id")]
    public int? ProductionLineId { get; set; }
}

public class UpdateProgressRequest
{
    [JsonPropertyName("percent")]
    public int Percent { get; set; }
}

public class SetLineStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class SetLineEfficiencyRequest
{
    [JsonPropertyName("factor")]
    public double Factor { get; set; }
}

public class ProductionCalculationRequest
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}