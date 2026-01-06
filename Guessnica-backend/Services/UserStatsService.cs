namespace Guessnica_backend.Services;

using Data;
using Dtos;
using Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserStatsService : IUserStatsService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public UserStatsService(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<UserStatsSummaryDto> GetMyStatsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new InvalidOperationException("User not found");

        var riddles = await _db.UserRiddles
            .Where(ur => ur.UserId == userId)
            .OrderBy(ur => ur.AssignedAt)
            .ToListAsync();

        var answered = riddles.Where(r => r.AnsweredAt != null).ToList();
        var correct = answered.Where(r => r.IsCorrect == true).ToList();
        
        int current = 0, best = 0;
        foreach (var r in answered.OrderBy(r => r.AnsweredAt))
        {
            if (r.IsCorrect == true)
            {
                current++;
                best = Math.Max(best, current);
            }
            else
            {
                current = 0;
            }
        }

        return new UserStatsSummaryDto
        {
            Assigned = riddles.Count,
            Answered = answered.Count,
            Correct = correct.Count,
            Incorrect = answered.Count - correct.Count,
            TotalScore = correct.Sum(r => r.Points ?? 0),
            AvgScore = correct.Any() ? correct.Average(r => r.Points ?? 0) : 0,
            CurrentStreak = current,
            BestStreak = best,
            AccountCreatedAt = user.CreatedAt
        };
    }
}
