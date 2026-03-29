using System.Security.Claims;
using Bookify.API.Data;
using Bookify.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bookify.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
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
            var instance =
                section.GetValue<string>("Instance") ?? "https://login.microsoftonline.com/";
            var tenantId = section.GetValue<string>("TenantId");
            var clientId = section.GetValue<string>("ClientId");
            var redirectUri = section.GetValue<string>("RedirectUri");
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                redirectUri =
                    Url.ActionLink(
                        action: nameof(OAuthCallback),
                        controller: "Auth",
                        values: null,
                        protocol: Request.Scheme,
                        host: Request.Host.Value
                    ) ?? string.Empty;
            }

            var instanceTrimmed = instance.TrimEnd('/');
            var authority = !string.IsNullOrEmpty(tenantId)
                ? $"{instanceTrimmed}/{tenantId}"
                : null;

            return Ok(
                new
                {
                    Instance = instanceTrimmed,
                    TenantId = tenantId ?? string.Empty,
                    ClientId = clientId ?? string.Empty,
                    Authority = authority,
                    RedirectUri = redirectUri,
                    Description = "Use Instance, TenantId, ClientId and Authority in MSAL. Register RedirectUri in Entra ID (Authentication → add URI) if this backend URL should receive the OAuth redirect; for classic SPA+PKCE, the SPA URL is usually the redirect instead.",
                }
            );
        }

        /// <summary>
        /// OAuth redirect target for Entra ID (optional). Register the same URL in Azure under Redirect URIs (Web or SPA, per your flow).
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        public IActionResult OAuthCallback(
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            [FromQuery] string? error_description
        )
        {
            if (!string.IsNullOrEmpty(error))
                return BadRequest(new { error, error_description });

            if (!string.IsNullOrEmpty(code))
            {
                return Ok(
                    new
                    {
                        message = "Authorization code received at the API. Standard MSAL browser login uses PKCE on the SPA redirect URI; exchanging the code on the server requires a confidential client and is not implemented here.",
                    }
                );
            }

            return Ok(
                new
                {
                    message = "Bookify API OAuth callback endpoint. Use this exact URL as a Redirect URI in Entra ID when you want Microsoft to return here after sign-in.",
                    url = Url.ActionLink(
                        nameof(OAuthCallback),
                        "Auth",
                        values: null,
                        protocol: Request.Scheme,
                        host: Request.Host.Value
                    ),
                }
            );
        }

        /// <summary>
        /// Returns the current user from the Entra JWT. Creates the user in the database if they don't exist (first login).
        /// Requires a valid Bearer token from Entra ID.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var entraId =
                User.FindFirstValue("oid")
                ?? User.FindFirstValue(
                    "http://schemas.microsoft.com/identity/claims/objectidentifier"
                );
            if (string.IsNullOrEmpty(entraId))
                return BadRequest(new { error = "Invalid token: missing user identifier (oid)." });

            var name =
                User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name") ?? "User";
            var email =
                User.FindFirstValue(ClaimTypes.Email)
                ?? User.FindFirstValue("preferred_username")
                ?? "";

            var user = await _context.Users.FirstOrDefaultAsync(u => u.EntraId == entraId);
            if (user == null)
            {
                user = new User
                {
                    EntraId = entraId,
                    Role = "User",
                    IsActive = true,
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
                return Forbid();

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
}
