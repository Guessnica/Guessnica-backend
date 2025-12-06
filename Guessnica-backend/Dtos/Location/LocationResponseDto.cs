namespace Guessnica_backend.Dtos.Location
{
    public class LocationResponseDto
    {
        public int Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public required string ImageUrl { get; set; }
        public string? ShortDescription { get; set; }
    }
}