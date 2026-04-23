using System.ComponentModel.DataAnnotations;

namespace ProductionControl.Models;

public class ProductionLine
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Stopped";

    public double EfficiencyFactor { get; set; } = 1.0;

    public int? CurrentWorkOrderId { get; set; }

    public WorkOrder? CurrentWorkOrder { get; set; }

    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}