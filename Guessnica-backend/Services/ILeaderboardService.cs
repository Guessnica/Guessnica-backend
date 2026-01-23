namespace Guessnica_backend.Services;

public interface ILeaderboardService
{
    Task<object> GetLeaderboardAsync(int days, int count);
    Task<object> GetUserRankAsync(string userId, int days);
}