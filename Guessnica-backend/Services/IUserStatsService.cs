namespace Guessnica_backend.Services;

using Dtos;

public interface IUserStatsService
{
    Task<UserStatsSummaryDto> GetMyStatsAsync(string userId);
}