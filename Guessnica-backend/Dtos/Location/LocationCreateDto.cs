namespace Guessnica_backend.Dtos.Location;

public class LocationCreateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public required string ImageUrl { get; set; }
}