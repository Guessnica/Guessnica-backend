namespace Guessnica_backend.Services;

using Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILocationService
{
    Task<IEnumerable<Location>> GetAllAsync();
    Task<Location> GetByIdAsync(int id);
    Task<Location> CreateAsync(Location loc);
    Task<Location> UpdateAsync(int id, Location updated);
    Task<bool> DeleteAsync(int id);
}