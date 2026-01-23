public class SubmitAnswerDto
{
    public int Points { get; set; }
    public double DistanceMeters { get; set; }
    public int TimeSeconds { get; set; }
    public bool IsCorrect { get; set; } 
    public decimal Latitude { get; set; } 
    public decimal Longitude { get; set; }

}