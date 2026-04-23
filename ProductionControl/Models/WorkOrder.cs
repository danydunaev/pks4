using System.ComponentModel.DataAnnotations;

namespace ProductionControl.Models;

public class WorkOrder
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int? ProductionLineId { get; set; }

    public ProductionLine? ProductionLine { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EstimatedEndDate { get; set; }

    [Required]
    public string Status { get; set; } = "Pending";

    [Range(0, 100)]
    public int ProgressPercent { get; set; }
}