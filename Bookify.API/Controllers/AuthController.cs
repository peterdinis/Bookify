using Bookify.API.Data;
using Bookify.API.DTOs;
using Bookify.API.Models;
using Bookify.API.Options;
using Bookify.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Bookify.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext context,
    SessionJwtService jwt,
    IOptions<SessionJwtOptions> jwtOptions
) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly SessionJwtService _jwt = jwt;
    private readonly SessionJwtOptions _jwtOpt = jwtOptions.Value;

    /// <summary>
    /// Vymení Entra object ID (z MSAL na fronte) za vlastný JWT. Backend neoveruje Entra tokeny.
    /// </summary>
    [HttpPost("session")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateSession([FromBody] SyncSessionRequest? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.EntraOid))
            return BadRequest(new { error = "EntraOid is required." });

        var oid = body.EntraOid.Trim();
        var name = string.IsNullOrWhiteSpace(body.Name) ? "User" : body.Name.Trim();
        var email = body.Email?.Trim() ?? string.Empty;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EntraId == oid);
        if (user == null)
        {
            user = new User
            {
                EntraId = oid,
                Role = "User",
                IsActive = true,
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        if (!user.IsActive)
            return Forbid();

        var accessToken = _jwt.CreateAccessToken(user, name, string.IsNullOrEmpty(email) ? null : email);

        return Ok(
            new
            {
                accessToken,
                tokenType = "Bearer",
                expiresIn = _jwtOpt.AccessTokenMinutes * 60,
                user = new
                {
                    user.Id,
                    user.EntraId,
                    user.Role,
                    user.IsActive,
                    Name = name,
                    Email = email,
                },
            }
        );
    }

    /// <summary>
    /// Aktuálny používateľ podľa Bookify JWT (nie Entra).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            return Unauthorized();

        var name = User.FindFirstValue(ClaimTypes.Name) ?? "User";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        return Ok(
            new
            {
                user.Id,
                user.EntraId,
                user.Role,
                user.IsActive,
                Name = name,
                Email = email,
            }
        );
    }
}
