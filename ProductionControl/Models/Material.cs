using System.ComponentModel.DataAnnotations;

namespace ProductionControl.Models;

public class Material
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    [Required]
    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal MinimalStock { get; set; }

    public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
}