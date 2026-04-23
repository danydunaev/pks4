using System.ComponentModel.DataAnnotations;

namespace TouristGuide.Models;

public class Attraction
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string History { get; set; } = string.Empty;

    [Required]
    [Url]
    public string ImageUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string OpeningHours { get; set; } = string.Empty;

    [StringLength(120)]
    public string? VisitCost { get; set; }

    public int CityId { get; set; }
    public City? City { get; set; }
}