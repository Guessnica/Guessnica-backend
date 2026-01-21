namespace Guessnica_backend.Controllers;

using Dtos;
using Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

[ApiController]
[Route("game")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly IGameService _game;
    private readonly IConfiguration _config;

    public GameController(IGameService game, IConfiguration config)
    {
        _game = game;
        _config = config;
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily()
    {
        var userId =
            User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var dailyHourUtc = _config.GetValue<int>("Game:DailyRiddleHourUtc", 0);

        var ur = await _game.GetDailyRiddleAsync(userId, dailyHourUtc);

        return Ok(new DailyRiddleResponseDto
        {
            UserRiddleId = ur.Id,
            RiddleId = ur.RiddleId,
            ImageUrl = ur.Riddle.Location.ImageUrl,
            Description = ur.Riddle.Description,
            Difficulty = (int)ur.Riddle.Difficulty,
            TimeLimitSeconds = ur.Riddle.TimeLimitSeconds,
            MaxDistanceMeters = ur.Riddle.MaxDistanceMeters
        });
    }
}