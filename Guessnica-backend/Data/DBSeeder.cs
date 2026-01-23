using Guessnica_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Guessnica_backend.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Riddles.AnyAsync())
            return;
        var rynek = new Location
        {
            Latitude = 51.2070m,
            Longitude = 16.1550m,
            ImageUrl = "/images/rynek.jpg",
            ShortDescription = "Rynek w Legnicy"
        };

        var zamek = new Location
        {
            Latitude = 51.2063m,
            Longitude = 16.1586m,
            ImageUrl = "/images/zamek.jpg",
            ShortDescription = "Zamek Piastowski"
        };

        var park = new Location
        {
            Latitude = 51.2088m,
            Longitude = 16.1502m,
            ImageUrl = "/images/park.jpg",
            ShortDescription = "Park Miejski"
        };

        db.Locations.AddRange(rynek, zamek, park);
        await db.SaveChangesAsync();
        var riddles = new List<Riddle>
        {
            new()
            {
                Description = "Jesteś w samym sercu Legnicy. Spójrz na kolorowe kamienice.",
                Difficulty = RiddleDifficulty.Easy,
                TimeLimitSeconds = 120,
                MaxDistanceMeters = 300,
                LocationId = rynek.Id
            },
            new()
            {
                Description = "Historyczna siedziba Piastów. Gdzie jesteś?",
                Difficulty = RiddleDifficulty.Medium,
                TimeLimitSeconds = 90,
                MaxDistanceMeters = 200,
                LocationId = zamek.Id
            },
            new()
            {
                Description = "Dużo zieleni, alejki i cisza. To miejsce zna każdy legniczanin.",
                Difficulty = RiddleDifficulty.Hard,
                TimeLimitSeconds = 60,
                MaxDistanceMeters = 150,
                LocationId = park.Id
            }
        };

        db.Riddles.AddRange(riddles);
        await db.SaveChangesAsync();
    }
}
