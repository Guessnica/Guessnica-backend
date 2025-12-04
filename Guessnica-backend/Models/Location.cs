namespace Guessnica_backend.Models;

public class Location
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public required string ImageUrl { get; set; }
}