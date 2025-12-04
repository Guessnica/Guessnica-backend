using Guessnica_backend.Data;
using Guessnica_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Guessnica_backend.Services;

public class LocationService : ILocationService
{
    private readonly AppDbContext _db;

    public LocationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Location>> GetAllAsync()
    {
        return await _db.Locations.ToListAsync();
    }

    public async Task<Location> GetByIdAsync(int id)
    {
        return await _db.Locations.FindAsync(id);
    }

    public async Task<Location> CreateAsync(Location loc)
    {
        _db.Locations.Add(loc);
        await _db.SaveChangesAsync();
        return loc;
    }

    public async Task<Location> UpdateAsync(int id, Location updated)
    {
        var loc = await _db.Locations.FindAsync(id);
        if (loc == null) return null;

        loc.Latitude = updated.Latitude;
        loc.Longitude = updated.Longitude;
        loc.ImageUrl = updated.ImageUrl;

        await _db.SaveChangesAsync();
        return loc;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var loc = await _db.Locations.FindAsync(id);
        if (loc == null) return false;

        _db.Locations.Remove(loc);
        await _db.SaveChangesAsync();
        return true;
    }
}