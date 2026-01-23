namespace Guessnica_backend.Services;

using Data;
using Microsoft.EntityFrameworkCore;

public class LeaderboardService : ILeaderboardService
{
    private readonly AppDbContext _db;

    public LeaderboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetLeaderboardAsync(int days, int count)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var leaderboard = await _db.UserRiddles
            .Where(ur => ur.AnsweredAt >= since && ur.IsCorrect == true)
            .GroupBy(ur => ur.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalPoints = g.Sum(ur => ur.Points ?? 0),
                CorrectAnswers = g.Count()
            })
            .OrderByDescending(x => x.TotalPoints)
            .Take(count)
            .ToListAsync();

        var userIds = leaderboard.Select(l => l.UserId).ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.AvatarUrl })
            .ToListAsync();

        var result = leaderboard.Select((entry, index) => new
        {
            Rank = index + 1,
            UserId = entry.UserId,
            DisplayName = users.FirstOrDefault(u => u.Id == entry.UserId)?.DisplayName ?? "Unknown",
            AvatarUrl = users.FirstOrDefault(u => u.Id == entry.UserId)?.AvatarUrl,
            TotalPoints = entry.TotalPoints,
            CorrectAnswers = entry.CorrectAnswers
        }).ToList();

        return result;
    }
    public async Task<object> GetUserRankAsync(string userId, int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var allRanks = await _db.UserRiddles
            .Where(ur => ur.AnsweredAt >= since && ur.IsCorrect == true)
            .GroupBy(ur => ur.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalPoints = g.Sum(ur => ur.Points ?? 0)
            })
            .OrderByDescending(x => x.TotalPoints)
            .ToListAsync();

        var userEntry = allRanks.FirstOrDefault(r => r.UserId == userId);
        if (userEntry == null)
        {
            return new
            {
                Rank = 0,
                TotalPoints = 0,
                TotalUsers = allRanks.Count
            };
        }

        var rank = allRanks.IndexOf(userEntry) + 1;

        return new
        {
            Rank = rank,
            TotalPoints = userEntry.TotalPoints,
            TotalUsers = allRanks.Count
        };
    }
}