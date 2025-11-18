using Guessnica_backend.Models;
using Guessnica_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Guessnica_backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Guessnica_backend.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<AppUser> userManager,
                          SignInManager<AppUser> signInManager,
                          IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Logowanie klasyczne za pomocą e-maila i hasła.
    /// </summary>
    /// <param name="model">Dane logowania (email, hasło).</param>
    [AllowAnonymous]
    [HttpPost("login")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing email",
                Detail = "Email is required",
                Extensions = { ["code"] = "missing_email" }
            });
        }

        if (string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing password",
                Detail = "Password is required",
                Extensions = { ["code"] = "missing_password" }
            });
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials",
                Detail = "Invalid email or password",
                Extensions = { ["code"] = "invalid_credentials" }
            });
        }

        if (!user.EmailConfirmed)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Email not confirmed",
                Detail = "Please confirm your email before logging in.",
                Extensions = { ["code"] = "email_not_confirmed" }
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials",
                Detail = "Invalid email or password",
                Extensions = { ["code"] = "invalid_credentials" }
            });
        }
        
        await _userManager.UpdateSecurityStampAsync(user);

        var (token, expiresAt) = await GenerateJwtTokenAsync(user);
        return Ok(new TokenResponseDto { Token = token, ExpiresAt = expiresAt });
    }

    /// <summary>
    /// Logowanie przez Facebooka przy użyciu AccessToken.
    /// </summary>
    /// <param name="dto">AccessToken z Facebooka.</param>
    /// <param name="fb">Serwis walidujący token FB.</param>
    [AllowAnonymous]
    [HttpPost("facebook")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginDto dto, [FromServices] IFacebookAuthService fb)
    {
        if (string.IsNullOrWhiteSpace(dto.AccessToken))
        {
            return BadRequest(new ProblemDetails {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid request",
                Detail = "AccessToken is required",
                Extensions = { ["code"] = "missing_access_token" }
            });
        }

        var (ok, fbUserId) = await fb.ValidateAccessTokenAsync(dto.AccessToken);
        if (!ok || string.IsNullOrEmpty(fbUserId))
        {
            return Unauthorized(new ProblemDetails {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid Facebook token",
                Detail = "Token is invalid or not issued for this app.",
                Extensions = { ["code"] = "invalid_facebook_token" }
            });
        }

        var profile = await fb.GetUserInfoAsync(dto.AccessToken);
        if (profile is null)
        {
            return Unauthorized(new ProblemDetails {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Facebook profile error",
                Detail = "Cannot fetch user profile.",
                Extensions = { ["code"] = "facebook_profile_error" }
            });
        }

        var email = profile.Email;
        var displayName = profile.Name ?? "Facebook User";

        var existingByLogin = await _userManager.FindByLoginAsync("Facebook", fbUserId);
        AppUser? user = existingByLogin;

        if (user == null && !string.IsNullOrEmpty(email))
            user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            var effectiveEmail = email ?? $"fb_{fbUserId}@no-email.facebook.local";
            user = new AppUser
            {
                UserName = effectiveEmail,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true
            };
            var createRes = await _userManager.CreateAsync(user);
            if (!createRes.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "User creation failed",
                    Detail = string.Join("; ", createRes.Errors.Select(e => $"{e.Code}: {e.Description}")),
                    Extensions = { ["code"] = "user_create_failed" }
                });
            }

            await _userManager.AddToRoleAsync(user, "User");
        }
        else
        {
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
        }

        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == "Facebook" && l.ProviderKey == fbUserId))
        {
            await _userManager.AddLoginAsync(user, new UserLoginInfo("Facebook", fbUserId, "Facebook"));
        }
        
        if (string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(email))
        {
            var byEmail = await _userManager.FindByEmailAsync(email);
            if (byEmail == null || byEmail.Id == user.Id)
            {
                user.Email = email;
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }
        }
        
        await _userManager.UpdateSecurityStampAsync(user);

        var (token, expiresAt) = await GenerateJwtTokenAsync(user);
        return Ok(new TokenResponseDto { Token = token, ExpiresAt = expiresAt });
    }

    /// <summary>
    /// Zwraca podstawowe informacje o aktualnie zalogowanym użytkowniku.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MeResponseDto>> Me()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new ProblemDetails { Title = "Invalid token", Status = 401 });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new ProblemDetails { Title = "User not found", Status = 404 });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new MeResponseDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Roles = roles.ToArray()
        });
    }

    /// <summary>
    /// „Wylogowanie” po stronie API (klient powinien po prostu porzucić JWT).
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult Logout()
    {
        return Ok(new
        {
            message = "Logged out successfully (client should discard JWT)."
        });
    }
    
    [AllowAnonymous]
    [HttpPost("request-reset")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestReset(
        [FromBody] RequestPasswordResetDto dto,
        [FromServices] IAppEmailSender emailSender,
        [FromServices] AppDbContext db)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        if (user is not null)
        {
            var hasLocalPwd = await _userManager.HasPasswordAsync(user);
            if (!hasLocalPwd)
            {
                return BadRequest(new ProblemDetails {
                    Status = 400, Title = "External login only",
                    Detail = "Account uses external login (Facebook). Use Facebook to sign in.",
                    Extensions = { ["code"] = "external_login_only" }
                });
            }
            
            var now = DateTime.UtcNow;
            var active = await db.UserVerificationCodes
                .Where(x => x.UserId == user.Id
                            && x.Purpose == "password_reset"
                            && x.UsedAtUtc == null
                            && x.ExpiresAtUtc > now)
                .ToListAsync();
            if (active.Count > 0)
            {
                db.UserVerificationCodes.RemoveRange(active);
                await db.SaveChangesAsync();
            }
            
            var code = Generate6DigitCode();
            var entity = new UserVerificationCode
            {
                UserId = user.Id,
                Purpose = "password_reset",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            };
            entity.CodeHash = HashCode(code, entity.Id);

            db.UserVerificationCodes.Add(entity);
            await db.SaveChangesAsync();
            
            var body = $"Twój kod resetu hasła do Guessnica: {code}\nKod ważny 15 minut.";
            await emailSender.SendAsync(email, "Guessnica — kod resetu hasła", body);
        }
        
        return Ok(new { message = "If the email exists, a code has been sent." });
    }
    
    [AllowAnonymous]
    [HttpPost("verify-reset-code")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> VerifyResetCode(
        [FromBody] VerifyResetCodeDto dto,
        [FromServices] AppDbContext db)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Unauthorized(new ProblemDetails {
                Status = 401, Title = "Invalid code", Detail = "Invalid code or expired.",
                Extensions = { ["code"] = "invalid_code" }
            });

        var now = DateTime.UtcNow;
        var candidate = await db.UserVerificationCodes
            .Where(x => x.UserId == user.Id
                        && x.Purpose == "password_reset"
                        && x.UsedAtUtc == null
                        && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (candidate is null)
            return Unauthorized(new ProblemDetails {
                Status = 401, Title = "Invalid code", Detail = "Invalid code or expired.",
                Extensions = { ["code"] = "invalid_code" }
            });

        if (candidate.Attempts >= 5)
            return Unauthorized(new ProblemDetails {
                Status = 401, Title = "Too many attempts", Detail = "Code locked.",
                Extensions = { ["code"] = "too_many_attempts" }
            });

        candidate.Attempts += 1;

        var expected = HashCode(dto.Code, candidate.Id);
        if (!string.Equals(expected, candidate.CodeHash, StringComparison.Ordinal))
        {
            await db.SaveChangesAsync();
            return Unauthorized(new ProblemDetails {
                Status = 401, Title = "Invalid code", Detail = "Invalid code or expired.",
                Extensions = { ["code"] = "invalid_code" }
            });
        }
        
        candidate.UsedAtUtc = now;
        candidate.ResetSessionId = Guid.NewGuid();
        candidate.ResetSessionExpiresAtUtc = now.AddMinutes(15);
        candidate.IdentityResetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        await db.SaveChangesAsync();

        return Ok(new
        {
            resetSessionId = candidate.ResetSessionId,
            expiresAt = candidate.ResetSessionExpiresAtUtc
        });
    }
    
    [AllowAnonymous]
    [HttpPost("set-new-password")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetNewPassword(
        [FromBody] SetNewPasswordDto dto,
        [FromServices] AppDbContext db)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Unauthorized(new ProblemDetails {
                Status = 401, Title = "Invalid session", Detail = "Invalid or expired session.",
                Extensions = { ["code"] = "invalid_session" }
            });

        var now = DateTime.UtcNow;

        var record = await db.UserVerificationCodes
            .Where(x => x.UserId == user.Id
                        && x.Purpose == "password_reset"
                        && x.ResetSessionId == dto.ResetSessionId
                        && x.ResetSessionExpiresAtUtc != null
                        && x.ResetSessionExpiresAtUtc > now
                        && x.IdentityResetToken != null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (record is null)
            return Unauthorized(new ProblemDetails {
                Status = 401, Title = "Invalid session", Detail = "Invalid or expired session.",
                Extensions = { ["code"] = "invalid_session" }
            });

        var res = await _userManager.ResetPasswordAsync(user, record.IdentityResetToken!, dto.NewPassword);
        if (!res.Succeeded)
        {
            return BadRequest(new ProblemDetails {
                Status = 400, Title = "Invalid password",
                Detail = string.Join("; ", res.Errors.Select(e => $"{e.Code}: {e.Description}")),
                Extensions = { ["code"] = "invalid_password" }
            });
        }
        
        record.ResetSessionExpiresAtUtc = now;
        await _userManager.UpdateSecurityStampAsync(user);
        await db.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully." });
    }

    private async Task<(string token, DateTime expiresAt)> GenerateJwtTokenAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var stamp = await _userManager.GetSecurityStampAsync(user);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("sstamp", stamp)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddHours(3);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
    
    private static string Generate6DigitCode()
    {
        Span<byte> b = stackalloc byte[4];
        RandomNumberGenerator.Fill(b);
        uint val = BitConverter.ToUInt32(b);
        return (val % 1_000_000u).ToString("D6");
    }

    private static string HashCode(string code, Guid salt)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes($"{salt}:{code}");
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}

public class LoginDto
{
    /// <example>test@example.com</example>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <example>Haslo123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class FacebookLoginDto
{
    /// <summary>AccessToken otrzymany z SDK/Facebook OAuth.</summary>
    [Required]
    public string AccessToken { get; set; } = string.Empty;
}

public class MeResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class TokenResponseDto
{
    /// <summary>JWT do użycia w nagłówku Authorization: Bearer &lt;token&gt;</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Data i czas wygaśnięcia (UTC).</summary>
    public DateTime ExpiresAt { get; set; }
}

public class RequestPasswordResetDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}

public class VerifyResetCodeDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6), MaxLength(6)] public string Code { get; set; } = string.Empty;
}

public record SetNewPasswordDto(
    [Required, EmailAddress] string Email,
    [Required] Guid ResetSessionId,
    [Required] string NewPassword
);