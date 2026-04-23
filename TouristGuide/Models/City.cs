using System.ComponentModel.DataAnnotations;

namespace TouristGuide.Models;

public class City
{
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Region { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Population { get; set; }

    [Required]
    [StringLength(400)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required]
    public string History { get; set; } = string.Empty;

    [Required]
    [Url]
    public string CoatOfArmsUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string ImageUrl { get; set; } = string.Empty;

    public ICollection<Attraction> Attractions { get; set; } = new List<Attraction>();
}