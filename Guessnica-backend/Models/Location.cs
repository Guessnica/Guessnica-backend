using System.ComponentModel.DataAnnotations;

namespace Guessnica_backend.Models;

public class Location
{
    public int Id { get; set; }
    [Required]
    [Range(-90, 90)]
    public required double Latitude { get; set; }
    [Required]
    [Range(-180, 180)]
    public required double Longitude { get; set; }
    public string ImageUrl { get; set; }
    [MaxLength(200)]
    public string? ShortDescription { get; set; }
}