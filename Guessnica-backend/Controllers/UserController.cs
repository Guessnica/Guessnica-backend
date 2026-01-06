namespace Guessnica_backend.Controllers;

using Services;
using Models;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("users")]
[Authorize]
public class UserController: ControllerBase
{
    private readonly IUserStatsService _stats;
    private readonly UserManager<AppUser> _userManager;
    
    public UserController(UserManager<AppUser> userManager,IUserStatsService stats)
    {
        _userManager = userManager;
        _stats = stats;
    }
    
    
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new MeResponseDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Roles = roles.ToArray()
        });
    }
    
    [HttpGet("me/stats")]
        public async Task<IActionResult> Stats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return Ok(await _stats.GetMyStatsAsync(userId));
        }
}