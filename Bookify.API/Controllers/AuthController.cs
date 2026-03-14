using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookify.API.Data;
using Bookify.API.Models;
using System.Security.Claims;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns Entra (Azure AD) configuration for client-side login. Use this to redirect users to Microsoft sign-in.
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public IActionResult GetAuthConfig([FromServices] IConfiguration configuration)
        {
            var section = configuration.GetSection("AzureAd");
            var instance = section.GetValue<string>("Instance") ?? "https://login.microsoftonline.com/";
            var tenantId = section.GetValue<string>("TenantId");
            var clientId = section.GetValue<string>("ClientId");

            return Ok(new
            {
                Instance = instance.TrimEnd('/'),
                TenantId = tenantId,
                ClientId = clientId,
                Authority = $"{instance.TrimEnd('/')}/{tenantId}",
                Description = "Use these values in your SPA/mobile app to configure MSAL and sign in with Entra ID."
            });
        }

        /// <summary>
        /// Returns the current user from the Entra JWT. Creates the user in the database if they don't exist (first login).
        /// Requires a valid Bearer token from Entra ID.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var entraId = User.FindFirstValue("oid") ?? User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (string.IsNullOrEmpty(entraId))
                return BadRequest(new { error = "Invalid token: missing user identifier (oid)." });

            var name = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "User";
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("preferred_username") ?? "";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EntraId == entraId);
            if (user == null)
            {
                user = new User
                {
                    EntraId = entraId,
                    Role = "User",
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
                return Forbid();

            return Ok(new
            {
                user.Id,
                user.EntraId,
                user.Role,
                user.IsActive,
                Name = name,
                Email = email
            });
        }
    }
}
