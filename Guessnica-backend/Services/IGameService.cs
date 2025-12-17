namespace Guessnica_backend.Services;

using Models;

public interface IGameService
{
    Task<UserRiddle> GetDailyRiddleAsync(string userId, int dailyHourUtc=0);
}